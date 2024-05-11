using NetLah.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static TConfigurationBuilder AddTransformConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, string sectionKey = "Transform")
        where TConfigurationBuilder : IConfigurationBuilder
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            throw new ArgumentException($"{nameof(sectionKey)} is required", nameof(sectionKey));
        }

        var configuration = configBuilder is IConfigurationRoot configurationRoot
            ? configurationRoot
            : configBuilder.Build();

        var configurationSection = configuration.GetSection(sectionKey);

        configBuilder.Add(new TransformConfigurationSource(configurationSection));

        return configBuilder;
    }
}
