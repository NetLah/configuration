using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public delegate IConfigurationBuilder AddFileDelegate(IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange);

public static class AddFileConfigurationSourceOptionsExtensions
{
    public static AddFileConfigurationSourceOptions AddProvider(this AddFileConfigurationSourceOptions options, string fileExtension, AddFileDelegate addFile)
    {
        options.AddProvider(fileExtension, (builder, source) => addFile(builder, source.Path, source.Optional, source.ReloadOnChange));
        return options;
    }

    public static AddFileConfigurationSourceOptions AddProvider(this AddFileConfigurationSourceOptions options, string fileExtension, ConfigureAddFileDelegate configureAddFile)
    {
        options.ConfigureAddFiles[fileExtension ?? throw new ArgumentNullException(nameof(fileExtension))] = configureAddFile ?? throw new ArgumentNullException(nameof(configureAddFile));
        return options;
    }

    public static AddFileConfigurationSourceOptions TryAddProvider(this AddFileConfigurationSourceOptions options, string fileExtension, ConfigureAddFileDelegate configureAddFile)
    {
        if (!options.ConfigureAddFiles.ContainsKey(fileExtension))
        {
            options.AddProvider(fileExtension, configureAddFile);
        }
        return options;
    }

    public static AddFileConfigurationSourceOptions TryAddProvider(this AddFileConfigurationSourceOptions options, string fileExtension, AddFileDelegate addFile)
    {
        if (!options.ConfigureAddFiles.ContainsKey(fileExtension))
        {
            options.AddProvider(fileExtension, addFile);
        }
        return options;
    }
}
