using NetLah.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddTransformConfiguration(this IConfigurationBuilder configBuilder, string transform = "Transform")
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            throw new ArgumentException($"{nameof(transform)} is required", nameof(transform));
        }
        var configuration = configBuilder.Build();
        var configurationSection = configuration.GetSection(transform);

        configBuilder.Add(new TransformConfigurationSource(configurationSection));

        return configBuilder;
    }

#if !NETSTANDARD
    public static ConfigurationManager AddTransformConfiguration(this ConfigurationManager manager, string transform = "Transform")
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            throw new ArgumentException($"{nameof(transform)} is required", nameof(transform));
        }
        var configurationSection = manager.GetSection(transform);

        IConfigurationBuilder configBuilder = manager;
        configBuilder.Add(new TransformConfigurationSource(configurationSection));

        return manager;
    }
#endif
}
