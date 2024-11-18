using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class ConnectionStringManager : IConnectionStringManager
{
    private IDictionary<string, ProviderConnectionString>? _connectionStrings;

    public ConnectionStringManager(IConfiguration configuration, string sectionName = "ConnectionStrings", Func<string, string>? keyNormalizer = null)
    {
#if NETSTANDARD
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
#else
        ArgumentNullException.ThrowIfNull(configuration);
#endif
        var configurationSection = string.IsNullOrEmpty(sectionName) ? configuration : configuration.GetSection(sectionName);
        var configurationKeyValue = ConnectionStringParser.ParseConfigurationKeyValue(configurationSection);
        Root = new ConnectionStringsRoot(configurationKeyValue, keyNormalizer);
    }

    internal ConnectionStringManager(ConnectionStringsRoot root, ProviderName? providerName)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Provider = providerName;
    }

    internal ConnectionStringsRoot Root { get; }

    public ProviderName? Provider { get; }

    public IConnectionStringManager CloneWithProvider(object? selectedProvider)
    {
        return new ConnectionStringManager(Root, ParseSelectProviderName(selectedProvider));
    }

    public IConnectionStringManager CloneWithKeyPreserveSpace()
    {
        return Root.KeyNormalizer == ConnectionStringsRoot.KeyPreserveSpaceNormalizer ?
                this :
                new ConnectionStringManager(new ConnectionStringsRoot(Root.Configuration, ConnectionStringsRoot.KeyPreserveSpaceNormalizer), Provider);
    }

    public IDictionary<string, ProviderConnectionString> ConnectionStrings => _connectionStrings ??= Root[Provider];

    public ProviderConnectionString? this[string? connectionName, params string?[]? connectionNames]
    {
        get
        {
            var names = connectionNames == null ? [connectionName] : connectionNames.Prepend(connectionName);
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

    internal static ProviderName? ParseSelectProviderName(object? selectingProvider)
    {
        return selectingProvider == null
            ? default
            : selectingProvider is DbProviders select1
            ? new ProviderName(select1)
            : selectingProvider is string selectingProviderStr
            ? Enum.TryParse<DbProviders>(selectingProviderStr, ignoreCase: true, out var select2)
                ? new ProviderName(select2)
                : !string.IsNullOrWhiteSpace(selectingProviderStr) ?
                new ProviderName(selectingProviderStr) :
                default
            : throw new InvalidOperationException($"'selectingProvider' only supported type DbProviders or System.String (provided type '{selectingProvider.GetType().FullName}')");
    }
}
