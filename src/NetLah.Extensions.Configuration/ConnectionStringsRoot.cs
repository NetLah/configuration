using System.Collections.Concurrent;

namespace NetLah.Extensions.Configuration;

internal class ConnectionStringsRoot(KeyValuePair<string, string>[] configuration,
    Func<string, string>? keyNormalizer,
    Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName?, IDictionary<string, ProviderConnectionString>>? factory = null)
{
    public KeyValuePair<string, string>[] Configuration { get; } = configuration;

    public Func<string, string> KeyNormalizer { get; } = keyNormalizer ?? KeyTrimNormalizer;

    public Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName?, IDictionary<string, ProviderConnectionString>> Factory { get; } = factory ?? ConnectionStringFactory;

    public IDictionary<string, ProviderConnectionString>? Default { get; private set; }   // for providerName is null

    public ConcurrentDictionary<ProviderName, IDictionary<string, ProviderConnectionString>> Cache { get; } = new ConcurrentDictionary<ProviderName, IDictionary<string, ProviderConnectionString>>(ProviderNameComparer.Instance);

    public IDictionary<string, ProviderConnectionString> this[ProviderName? providerName]
    {
        get
        {
            return providerName == null ? (Default ??= ConnStrFactory()) : Cache.GetOrAdd(providerName, _ => ConnStrFactory());
            IDictionary<string, ProviderConnectionString> ConnStrFactory() => Factory(Configuration, KeyNormalizer, providerName);
        }
    }

    internal static string KeyTrimNormalizer(string s) => s.Trim();

    public static string KeyPreserveSpaceNormalizer(string s) => s;

    internal static IDictionary<string, ProviderConnectionString> ConnectionStringFactory(KeyValuePair<string, string>[] configuration,
        Func<string, string> keyNormalizer,
        ProviderName? providerName)
        => new ConnectionStringParser(configuration, providerName, keyNormalizer).Parse();
}
