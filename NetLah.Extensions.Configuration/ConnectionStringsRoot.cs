using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NetLah.Extensions.Configuration
{
    internal class ConnectionStringsRoot
    {
        public ConnectionStringsRoot(KeyValuePair<string, string>[] configuration,
            Func<string, string> keyNormalizer,
            Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName, IDictionary<string, ProviderConnectionString>> factory = null)
        {
            Configuration = configuration;
            KeyNormalizer = keyNormalizer ?? KeyTrimNormalizer;
            Factory = factory ?? ConnectionStringFactory;
            Cache = new ConcurrentDictionary<ProviderName, IDictionary<string, ProviderConnectionString>>(ProviderNameComparer.Instance);
        }

        public KeyValuePair<string, string>[] Configuration { get; }

        public Func<string, string> KeyNormalizer { get; }

        public Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName, IDictionary<string, ProviderConnectionString>> Factory { get; }

        public IDictionary<string, ProviderConnectionString> Default { get; private set; }   // for providerName is null

        public ConcurrentDictionary<ProviderName, IDictionary<string, ProviderConnectionString>> Cache { get; }

        public IDictionary<string, ProviderConnectionString> this[ProviderName providerName]
        {
            get
            {
                if (providerName == null)
                    return Default ??= ConnStrFactory();

                return Cache.GetOrAdd(providerName, _ => ConnStrFactory());

                IDictionary<string, ProviderConnectionString> ConnStrFactory() => Factory(Configuration, KeyNormalizer, providerName);
            }
        }

        internal static string KeyTrimNormalizer(string s) => s == null ? s : s.Trim();

        public static string KeyPreserveSpaceNormalizer(string s) => s;

        internal static IDictionary<string, ProviderConnectionString> ConnectionStringFactory(KeyValuePair<string, string>[] configuration,
            Func<string, string> keyNormalizer,
            ProviderName providerName)
            => new ConnectionStringParser(configuration, providerName, keyNormalizer).Parse();
    }
}
