using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Globalization;

namespace NetLah.Extensions.Configuration;

public static class ConnectionStringUtilities
{
    private static string? ConvertToString(object value) => value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);

    public static IConfiguration ToConfiguration(string connectionString)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new DbConnectionStringBuilder { ConnectionString = connectionString }
                .Cast<KeyValuePair<string, object>>()
                .ToDictionary(kv => kv.Key, kv => ConvertToString(kv.Value)))
            .Build();

    public static IConfiguration ToConfiguration(this ProviderConnectionString providerConnectionString)
        => providerConnectionString.Configuration ??= ToConfiguration(providerConnectionString.Value);

    public static TOptions? Get<TOptions>(string connectionString)
    {
        var configuration = ToConfiguration(connectionString);
        return configuration.Get<TOptions>();
    }

    public static TOptions? Get<TOptions>(this ProviderConnectionString providerConnectionString)
    {
        var configuration = providerConnectionString.ToConfiguration();
        return configuration.Get<TOptions>();
    }

    public static TOptions Bind<TOptions>(string connectionString, TOptions instance)
    {
        var configuration = ToConfiguration(connectionString);
        configuration.Bind(instance);
        return instance;
    }

    public static TOptions Bind<TOptions>(this ProviderConnectionString providerConnectionString, TOptions instance)
    {
        var configuration = providerConnectionString.ToConfiguration();
        configuration.Bind(instance);
        return instance;
    }
}
