using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration
{
    public class ConnectionStringManager : IConnectionStringManager
    {
        private IDictionary<string, ProviderConnectionString> _connectionStrings;

        public ConnectionStringManager(IConfiguration configuration, string sectionName = "ConnectionStrings", Func<string, string> keyNormalize = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            var configurationSection = string.IsNullOrEmpty(sectionName) ? configuration : configuration.GetSection(sectionName);
            var configurationKeyValue = ConnectionStringParser.ParseConfigurationKeyValue(configurationSection);
            Root = new ConnectionStringsRoot(configurationKeyValue, keyNormalize);
        }

        internal ConnectionStringManager(ConnectionStringsRoot root, ProviderName providerName)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Provider = providerName;
        }

        internal ConnectionStringsRoot Root { get; }

        public ProviderName Provider { get; }

        public IConnectionStringManager CloneWithProvider(object selectedProvider)
            => new ConnectionStringManager(Root, ParseSelectProviderName(selectedProvider));

        public IConnectionStringManager CloneWithKeyPreserveSpace()
            => Root.KeyNormalizer == ConnectionStringsRoot.KeyPreserveSpaceNormalizer ?
                this :
                new ConnectionStringManager(new ConnectionStringsRoot(Root.Configuration, ConnectionStringsRoot.KeyPreserveSpaceNormalizer), Provider);

        public IDictionary<string, ProviderConnectionString> ConnectionStrings => _connectionStrings ??= Root[Provider];

        public ProviderConnectionString this[string connectionName, params string[] connectionNames]
        {
            get
            {
                var names = connectionNames == null ? new[] { connectionName } : connectionNames.Prepend(connectionName);
                foreach (var item in names)
                {
                    if (item != null && ConnectionStrings.TryGetValue(Root.KeyNormalizer(item), out var result))
                    {
                        return result;
                    }
                }
                return default;
            }
        }

        internal static ProviderName ParseSelectProviderName(object selectingProvider)
        {
            if (selectingProvider == null)
                return default;

            if (selectingProvider is DbProviders select1)
                return new ProviderName(select1);

            if (selectingProvider is string selectingProviderStr)
            {
                if (Enum.TryParse<DbProviders>(selectingProviderStr, ignoreCase: true, out var select2))
                    return new ProviderName(select2);

                return !string.IsNullOrWhiteSpace(selectingProviderStr) ?
                    new ProviderName(selectingProviderStr) :
                    default;
            }

            throw new InvalidOperationException($"'selectingProvider' only supported type DbProviders or System.String (provided type '{selectingProvider.GetType().FullName}')");
        }
    }
}
