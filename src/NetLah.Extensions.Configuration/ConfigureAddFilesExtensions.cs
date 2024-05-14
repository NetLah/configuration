using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public delegate IConfigurationBuilder AddFileDelegate(IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange);

public static class ConfigureAddFilesExtensions
{
    public static TConfigureAddFiles AddProvider<TConfigureAddFiles>(this TConfigureAddFiles configureAddFiles, string fileExtension, AddFileDelegate addFile, bool resolveAbsolute = false)
        where TConfigureAddFiles : IConfigureAddFiles
    {
        return configureAddFiles.AddProvider(fileExtension, (builder, source) => addFile(builder, source.Path, source.Optional, source.ReloadOnChange), resolveAbsolute);
    }

    public static TConfigureAddFiles AddProvider<TConfigureAddFiles>(this TConfigureAddFiles configureAddFiles, string fileExtension, ConfigureAddFileDelegate configureAddFile, bool resolveAbsolute = false)
        where TConfigureAddFiles : IConfigureAddFiles
    {
        return configureAddFiles.AddProvider(fileExtension, new ConfigureAddFileOptions
        {
            ConfigureAction = configureAddFile ?? throw new ArgumentNullException(nameof(configureAddFile)),
            ResolveAbsolute = resolveAbsolute
        });
    }

    public static TConfigureAddFiles AddProvider<TConfigureAddFiles>(this TConfigureAddFiles configureAddFiles, string fileExtension, ConfigureAddFileOptions configureAddFileOptions)
        where TConfigureAddFiles : IConfigureAddFiles
    {
        configureAddFiles.ConfigureAddFiles[fileExtension ?? throw new ArgumentNullException(nameof(fileExtension))] = configureAddFileOptions ?? throw new ArgumentNullException(nameof(configureAddFileOptions));
        return configureAddFiles;
    }

    public static TConfigureAddFiles TryAddProvider<TConfigureAddFiles>(this TConfigureAddFiles configureAddFiles, string fileExtension, ConfigureAddFileDelegate configureAddFile, bool resolveAbsolute = false)
        where TConfigureAddFiles : IConfigureAddFiles
    {
        if (!configureAddFiles.ConfigureAddFiles.ContainsKey(fileExtension))
        {
            configureAddFiles.AddProvider(fileExtension, configureAddFile, resolveAbsolute);
        }
        return configureAddFiles;
    }

    public static TConfigureAddFiles TryAddProvider<TConfigureAddFiles>(this TConfigureAddFiles configureAddFiles, string fileExtension, AddFileDelegate addFile, bool resolveAbsolute = false)
          where TConfigureAddFiles : IConfigureAddFiles
    {
        if (!configureAddFiles.ConfigureAddFiles.ContainsKey(fileExtension))
        {
            configureAddFiles.AddProvider(fileExtension, addFile, resolveAbsolute);
        }
        return configureAddFiles;
    }
}
