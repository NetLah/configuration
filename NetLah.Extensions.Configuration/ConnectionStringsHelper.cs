using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration
{
    public class ConnectionStringsHelper
    {
        private static readonly Regex NameAndProvider = new("^(?<connectionName>.+)_(?<providerName>[^_]+)$", RegexOptions.Compiled);

        private readonly IConfigurationSection _configuration;
        private Dictionary<string, ConnectionStringInfo> _connectionStrings;

        public ConnectionStringsHelper(IConfiguration configuration, string connectionStringsSectionName = "ConnectionStrings")
        {
            _configuration = configuration?.GetSection(connectionStringsSectionName) ?? throw new ArgumentNullException(nameof(configuration));
        }

        public ConnectionStringInfo this[string connectionName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(connectionName))
                    throw new ArgumentException($"{nameof(connectionName)} is required");

                if (ConnectionStrings.TryGetValue(connectionName, out var result))
                    return result;

                return null;
            }
        }

        public Dictionary<string, ConnectionStringInfo> ConnectionStrings => _connectionStrings ??= ParseConnectionStrings(null);

        public Dictionary<string, ConnectionStringInfo> ParseConnectionStrings(object selectingProvider)
        {
            DbProviders? selectedProvider = null;
            string customTypeName = null;
            if (selectingProvider != null)
            {
                if (selectingProvider is DbProviders select1)
                {
                    selectedProvider = select1;
                }
                else if (selectingProvider is string selectingProviderStr && !string.IsNullOrWhiteSpace(selectingProviderStr))
                {
                    selectedProvider = DbProviders.Custom;
                    if (Enum.TryParse<DbProviders>(selectingProviderStr, ignoreCase: true, out var select2))
                    {
                        selectedProvider = select2;
                    }
                    else
                    {
                        customTypeName = selectingProviderStr;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"'selectingProvider' only supported type DbProviders or System.String (provided type '{selectingProvider.GetType().FullName}')");
                }
            }

            var lookup = _configuration
                .GetChildren()
                .Select(c => (normalizedKey: c.Key.ToUpperInvariant(), key: c.Key, value: c.Value))
                .GroupBy(kv => kv.normalizedKey)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var normalziedkeys = lookup.Keys.OrderBy(k => k.Length).ToArray();

            var conns = new List<(string name, ConnectionStringInfo connStr)>();

            foreach (var key1 in normalziedkeys)
            {
                if (lookup.Remove(key1, out var kv))
                {
                    var m = NameAndProvider.Match(kv.key);

                    if (m.Success)
                    {
                        var connectionName = m.Groups["connectionName"].Value;
                        var providerName = m.Groups["providerName"].Value;
                        Add(connectionName, providerName);
                    }
                    else if (lookup.Remove($"{kv.key}_ProviderName", out var provider))   // parse Microsoft Azure style
                    {
                        Add(kv.key, provider.value);
                    }
                    else
                    {
                        Add(kv.key, null);
                    }

                    void Add(string connectionName, string providerName)
                    {
                        var (provider, customProviderName) = ParseProviderName(providerName);

                        if (!selectedProvider.HasValue ||
                            (selectedProvider == provider &&
                                (provider != DbProviders.Custom || string.IsNullOrEmpty(customTypeName) || string.Equals(customProviderName, customTypeName, StringComparison.OrdinalIgnoreCase))))
                        {
                            if (provider == DbProviders.Custom &&
                                !string.IsNullOrEmpty(customTypeName) && string.Equals(customProviderName, customTypeName, StringComparison.OrdinalIgnoreCase))
                            {
                                customProviderName = customTypeName;
                            }

                            conns.Add((connectionName, new ConnectionStringInfo(kv.value, provider, customProviderName)));
                        }
                    }
                }
            }

            return conns
                .GroupBy(c => c.name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().connStr, StringComparer.OrdinalIgnoreCase);
        }

        // https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.EnvironmentVariables/src/EnvironmentVariablesConfigurationProvider.cs#L15-L18
        internal static (DbProviders, string) ParseProviderName(string providerName)
        {
            var provider = DbProviders.Custom;

            switch (providerName?.ToUpperInvariant())
            {
                case "SQLSERVER":
                case "MSSQL":
                case "SQLAZURE":
                case "SYSTEM.DATA.SQLCLIENT":
                    provider = DbProviders.SQLServer;
                    break;

                case "MYSQL":
                case "MYSQL.DATA.MYSQLCLIENT":
                    provider = DbProviders.MySQL;
                    break;

                case "POSTGRESQL":
                    provider = DbProviders.PostgreSQL;
                    break;
            }

            var customProviderName = provider == DbProviders.Custom && !string.IsNullOrWhiteSpace(providerName) ? providerName : null;

            return (provider, customProviderName);
        }
    }
}
