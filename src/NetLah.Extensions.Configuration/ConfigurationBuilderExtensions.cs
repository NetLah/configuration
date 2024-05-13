using Microsoft.Extensions.Configuration.Json;
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

    public static TConfigurationBuilder AddAddFileConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, string sectionKey = "AddFile", bool? throwIfNotSupport = null)
        where TConfigurationBuilder : IConfigurationBuilder
    {
        return configBuilder.AddAddFileConfiguration(options => options.ThrowIfNotSupport = throwIfNotSupport, sectionKey);
    }

    internal static TConfigurationBuilder AddAddFileConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, AddFileConfigurationSourceOptionsBuilder optionsBuilder)
    where TConfigurationBuilder : IConfigurationBuilder
    {
        return optionsBuilder == null
            ? throw new ArgumentNullException(nameof(optionsBuilder))
            : configBuilder.AddAddFileConfiguration(options =>
        {
            options.ThrowIfNotSupport = optionsBuilder.ThrowIfNotSupport;
        }, optionsBuilder.SectionKey);
    }

    public static TConfigurationBuilder AddAddFileConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, Action<AddFileConfigurationSourceOptions> configureOptions, string sectionKey = "AddFile")
        where TConfigurationBuilder : IConfigurationBuilder
    {
        var configuration = configBuilder is IConfigurationRoot configurationRoot
           ? configurationRoot
           : configBuilder.Build();

        var configurationSection = configuration.GetSection(sectionKey);

        var options = new AddFileConfigurationSourceOptions(configurationSection);
        configureOptions(options);

        return configBuilder.AddAddFileConfiguration(options);
    }

    internal static TConfigurationBuilder AddAddFileConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, AddFileConfigurationSourceOptions options)
        where TConfigurationBuilder : IConfigurationBuilder
    {
        var sources = configBuilder.Sources;
        var lastIndexJson = FindLastIndex(sources, s => s is JsonConfigurationSource js);   // && js.Path != "secrets.json")
        lastIndexJson = lastIndexJson >= 0 ? lastIndexJson + 1 : sources.Count;
        sources.Insert(lastIndexJson, new AddFileConfigurationSource(options));

        return configBuilder;
    }

    private static int FindLastIndex<T>(this IList<T> source, Predicate<T> match)
    {
        for (var i = source.Count - 1; i >= 0; i--)
        {
            if (match(source[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
