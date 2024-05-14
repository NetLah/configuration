using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
#if NETSTANDARD
// todo: rename to TConfigurationBuilder
using BuilderOrManager = Microsoft.Extensions.Configuration.IConfigurationBuilder;
#else
using BuilderOrManager = Microsoft.Extensions.Configuration.ConfigurationManager;
#endif

namespace NetLah.Extensions.Configuration;

public sealed class ConfigurationBuilderBuilder
{
    private readonly List<Action<IConfigurationBuilder>> _configureConfigActions = new();
    private readonly List<Action<IConfigurationBuilder>> _configurePostConfigActions = new();
    private string[]? _args;
    private Assembly? _assembly;
    private string? _basePath;
    private string? _environmentName;
    private BuilderOrManager? _builderOrManager;
    private IConfiguration? _configuration;
    private IEnumerable<KeyValuePair<string, string?>>? _initialData;
    private AddFileConfigurationSourceOptionsBuilder? _addFileOptionsBuilder;

    private BuilderOrManager ConfigureBuilder()
    {
#if NETSTANDARD
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection();
#else
        var configBuilder = new BuilderOrManager();
#endif
        configBuilder
            .AddEnvironmentVariables(prefix: "DOTNET_")
            .AddEnvironmentVariables(prefix: "ASPNETCORE_");

        if (_environmentName == null)
        {
            _environmentName = ((IConfigurationBuilder)configBuilder).Build()[HostDefaults.EnvironmentKey];
        }

        if (!string.IsNullOrEmpty(_basePath))
        {
            configBuilder.SetBasePath(_basePath);       // basePath cannot be null 
        }

        if (_configuration is { } configuration)
        {
            configBuilder.AddConfiguration(configuration, shouldDisposeConfiguration: false);
        }

        if (_initialData is { } initialData)
        {
            configBuilder.AddInMemoryCollection(initialData);
        }

        configBuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true, reloadOnChange: true);

        foreach (var buildAction in _configureConfigActions)
        {
            buildAction(configBuilder);
        }

        if (_assembly is { } assembly && string.Equals(EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddUserSecrets(assembly, optional: true, reloadOnChange: true);
        }

        configBuilder.AddEnvironmentVariables();
        if (_args is { Length: > 0 } args)
        {
            configBuilder.AddCommandLine(args);
        }

        foreach (var buildAction in _configurePostConfigActions)
        {
            buildAction(configBuilder);
        }

        return configBuilder;
    }

    private ConfigurationBuilderBuilder ResetBuilder()
    {
        _builderOrManager = null;
        return this;
    }

    public string EnvironmentName => _environmentName ?? Environments.Production;

    public string? ApplicationName => _assembly?.FullName;

#if NETSTANDARD
    public IConfigurationBuilder Builder => _builderOrManager ??= ConfigureBuilder();

    public IConfigurationRoot Build() => Builder.Build();
#else
    public ConfigurationManager Manager => _builderOrManager ??= ConfigureBuilder();

    [Obsolete("This property is obsolete. Use " + nameof(Manager) + " instead.")]
    public IConfigurationBuilder Builder => Manager;

    [Obsolete("This method is obsolete. Use property " + nameof(Manager) + " instead.")]
    public IConfigurationRoot Build() => Manager;
#endif

    public ConfigurationBuilderBuilder WithAddConfiguration(Action<IConfigurationBuilder> addConfiguration)
    {
        _configureConfigActions.Add(addConfiguration);
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithAddPostConfiguration(Action<IConfigurationBuilder> addConfiguration)
    {
        _configurePostConfigActions.Add(addConfiguration);
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithAppSecrets(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithAppSecrets<TStartup>() => WithAppSecrets(typeof(TStartup).Assembly);

    public ConfigurationBuilderBuilder WithBasePath(string basePath)
    {
        _basePath = !string.IsNullOrWhiteSpace(basePath) ? Path.GetFullPath(basePath) : null;
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithBaseDirectory() => WithBasePath(AppDomain.CurrentDomain.BaseDirectory);

    public ConfigurationBuilderBuilder WithClearAddedConfiguration(bool clear = true)
    {
        if (clear)
        {
            _configureConfigActions.Clear();
        }

        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithCommandLines(params string[]? args)
    {
        _args = args;
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithCurrentDirectory() => WithBasePath(Directory.GetCurrentDirectory());

    public ConfigurationBuilderBuilder WithEnvironment(string environmentName)
    {
        _environmentName = environmentName;
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithInMemory(IEnumerable<KeyValuePair<string, string?>> initialData)
    {
        _initialData = initialData;
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithAddFileConfiguration(Action<AddFileConfigurationSourceOptions>? configureOptions = null, string sectionKey = "AddFile", bool? throwIfNotSupport = null)
    {
        if (!_configurePostConfigActions.Contains(ConfigureAddFile))
        {
            _configurePostConfigActions.Add(ConfigureAddFile);
        }
        _addFileOptionsBuilder ??= new AddFileConfigurationSourceOptionsBuilder();
        _addFileOptionsBuilder.SectionKey = sectionKey;
        _addFileOptionsBuilder.ThrowIfNotSupport = throwIfNotSupport;
        configureOptions?.Invoke(_addFileOptionsBuilder);
        return ResetBuilder();
    }

    public ConfigurationBuilderBuilder WithTransformConfiguration(string sectionKey = "Transform")
    {
        return WithAddPostConfiguration(builder => builder.AddTransformConfiguration(sectionKey));
    }

    public ConfigurationBuilderBuilder WithMapConfiguration(string sectionKey = "MapConfiguration")
    {
        return WithAddPostConfiguration(builder => builder.AddMapConfiguration(sectionKey));
    }

    public static ConfigurationBuilderBuilder Create<TStartup>(string[]? args = null)
        => new ConfigurationBuilderBuilder()
            .WithCommandLines(args)
            .WithAppSecrets<TStartup>();

    public static ConfigurationBuilderBuilder Create(Assembly assembly, string[]? args = null)
        => new ConfigurationBuilderBuilder()
            .WithCommandLines(args)
            .WithAppSecrets(assembly);

    public static ConfigurationBuilderBuilder Create(string[]? args = null)
        => new ConfigurationBuilderBuilder()
            .WithCommandLines(args);

    private void ConfigureAddFile(IConfigurationBuilder builder)
    {
        if (_addFileOptionsBuilder != null)
        {
            builder.AddAddFileConfiguration(_addFileOptionsBuilder);
        }
    }
}
