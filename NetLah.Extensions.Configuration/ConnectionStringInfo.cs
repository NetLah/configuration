using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration
{
    public class ConnectionStringInfo
    {
        public ConnectionStringInfo(string connectionString, DbProviders provider = DbProviders.Custom, string custom = null)
        {
            ConnectionString = connectionString;
            Provider = provider;
            Custom = custom;
        }

        public string ConnectionString { get; }
        public DbProviders Provider { get; }
        public string Custom { get; }
    }

    public class ConnectionStringComplexInfo : ConnectionStringInfo
    {
        public ConnectionStringComplexInfo(DbProviders provider = DbProviders.Custom, string custom = null, IConfigurationSection configuration = null)
            : base(null, provider, custom)
        {
            Configuration = configuration;
        }

        public IConfigurationSection Configuration { get; }

        internal static ConnectionStringInfo Create(string connectionString, DbProviders provider = DbProviders.Custom, string custom = null, IConfigurationSection configuration = null)
            => connectionString == null && configuration != null ?
                new ConnectionStringComplexInfo(provider, custom, configuration) :
                new ConnectionStringInfo(connectionString, provider, custom);
    }
}
