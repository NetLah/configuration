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
}
