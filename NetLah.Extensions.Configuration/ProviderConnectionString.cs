using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration
{
    /// <summary>
    /// Provider and connection string
    /// </summary>
    public sealed class ProviderConnectionString : ProviderName
    {
        private string _expanded;

        public ProviderConnectionString(string name, string connectionString, DbProviders provider = DbProviders.Custom, string custom = null)
            : base(provider, custom)
        {
            Name = name;
            Raw = connectionString;
        }

        /// <summary>
        /// Connection string name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Expanded connection string value
        /// </summary>
        public string Value { get => _expanded ?? Raw; set => _expanded = value; }

        /// <summary>
        /// Raw connection string value
        /// </summary>
        public string Raw { get; }

        public IConfiguration Configuration { get; internal set; }
    }
}
