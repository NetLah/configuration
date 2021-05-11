using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NetLah.Extensions.Configuration
{
    public sealed class ConfigurationBuilderBuilder
    {
        private readonly List<Action<IConfigurationBuilder>> _configureConfigActions = new();
        private string[] _args;
        private Assembly _assembly;
        private string _basePath;
        private string _environmentName;
        private IConfigurationBuilder _configurationBuilder;
        private IConfiguration _hostConfig;

        public ConfigurationBuilderBuilder()
        {
        }

        private IConfigurationBuilder ConfigureBuilder()
        {
            if (_environmentName == null && _hostConfig == null)
            {
                IConfigurationBuilder hostConfigBuilder = new ConfigurationBuilder()
                     .AddInMemoryCollection()
                     .AddEnvironmentVariables(prefix: "DOTNET_")
                     .AddEnvironmentVariables(prefix: "ASPNETCORE_");
                _hostConfig = hostConfigBuilder.Build();
            }

            _environmentName ??= _hostConfig[HostDefaults.EnvironmentKey];

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();

            if (!string.IsNullOrEmpty(_basePath))
            {
                configBuilder.SetBasePath(_basePath);       // basePath cannot be null 
            }

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

        public string ApplicationName => _assembly?.FullName;

        public IConfigurationBuilder Builder => _configurationBuilder ??= ConfigureBuilder();

        public IConfigurationRoot Build() => Builder.Build();

        public ConfigurationBuilderBuilder WithBasePath(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            return ResetBuilder();
        }

        public ConfigurationBuilderBuilder WithCurrentDirectory() => WithBasePath(Directory.GetCurrentDirectory());

        public ConfigurationBuilderBuilder WithBaseDirectory() => WithBasePath(AppDomain.CurrentDomain.BaseDirectory);

        public ConfigurationBuilderBuilder WithCommandLines(string[] args)
        {
            _args = args;
            return ResetBuilder();
        }

        public ConfigurationBuilderBuilder WithAppSecrets(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            return ResetBuilder();
        }

        public ConfigurationBuilderBuilder WithAppSecrets<TStartup>() => WithAppSecrets(typeof(TStartup).Assembly);

        public ConfigurationBuilderBuilder WithEnvironment(string environmentName)
        {
            _environmentName = environmentName;
            return ResetBuilder();
        }

        public ConfigurationBuilderBuilder WithAddConfiguration(Action<IConfigurationBuilder> addConfiguration)
        {
            _configureConfigActions.Add(addConfiguration);
            return ResetBuilder();
        }

        public static ConfigurationBuilderBuilder Create<TStartup>(string[] args = null)
            => new ConfigurationBuilderBuilder()
                .WithCommandLines(args)
                .WithAppSecrets<TStartup>();
    }
}
