using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace NetLah.Extensions.Configuration;

public sealed class ConfigurationBuilderBuilder
{
    private readonly List<Action<IConfigurationBuilder>> _configureConfigActions = new();
    private string[]? _args;
    private Assembly? _assembly;
    private string? _basePath;
    private string? _environmentName;
    private IConfigurationBuilder? _configurationBuilder;
    private IConfiguration? _hostConfig;
    private IConfiguration? _configuration;
    private IEnumerable<KeyValuePair<string, string>>? _initialData;

    private IConfigurationBuilder ConfigureBuilder()
    {
        if (_environmentName == null && _hostConfig == null)
        {
            var hostConfigBuilder = new ConfigurationBuilder()
                 .AddInMemoryCollection()
                 .AddEnvironmentVariables(prefix: "DOTNET_")
                 .AddEnvironmentVariables(prefix: "ASPNETCORE_");
            _hostConfig = hostConfigBuilder.Build();
        }

        _environmentName ??= _hostConfig?[HostDefaults.EnvironmentKey];

        var configBuilder = (IConfigurationBuilder)new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(_basePath))
        {
            configBuilder.SetBasePath(_basePath);       // basePath cannot be null 
        }

        if (_configuration is { } configuration)
            configBuilder.AddConfiguration(configuration, shouldDisposeConfiguration: false);

        if (_initialData is { } initialData)
            configBuilder.AddInMemoryCollection(initialData);

        if (_hostConfig != null)
        {
            configBuilder.AddConfiguration(_hostConfig, shouldDisposeConfiguration: true);
        }

        configBuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true, reloadOnChange: true);

        foreach (Action<IConfigurationBuilder> buildAction in _configureConfigActions)
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

        return configBuilder;
    }

    private ConfigurationBuilderBuilder ResetBuilder()
    {
        _configurationBuilder = null;
        return this;
    }

    public string EnvironmentName => _environmentName ?? Environments.Production;

    public string? ApplicationName => _assembly?.FullName;

    public IConfigurationBuilder Builder => _configurationBuilder ??= ConfigureBuilder();

    public IConfigurationRoot Build() => Builder.Build();

    public ConfigurationBuilderBuilder WithAddConfiguration(Action<IConfigurationBuilder> addConfiguration)
    {
        _configureConfigActions.Add(addConfiguration);
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
            _configureConfigActions.Clear();
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

    public ConfigurationBuilderBuilder WithInMemory(IEnumerable<KeyValuePair<string, string>> initialData)
    {
        _initialData = initialData;
        return ResetBuilder();
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
}
