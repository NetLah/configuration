﻿using Microsoft.Extensions.Configuration.Json;
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

    public static TConfigurationBuilder AddAddFileConfiguration<TConfigurationBuilder>(this TConfigurationBuilder configBuilder, string sectionKey = "AddFile", bool throwIfNotSupport = false)
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

        var sources = configBuilder.Sources;
        var lastIndexJson = FindLastIndex(sources, s => s is JsonConfigurationSource js && js.Path != "secrets.json");
        lastIndexJson = lastIndexJson >= 0 ? lastIndexJson + 1 : sources.Count;
        sources.Insert(lastIndexJson, new AddFileConfigurationSource(configurationSection, throwIfNotSupport));

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
