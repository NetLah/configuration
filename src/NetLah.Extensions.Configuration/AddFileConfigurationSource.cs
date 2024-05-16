using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#if NETSTANDARD
using TConfigurationBuilder = Microsoft.Extensions.Configuration.IConfigurationBuilder;
#else
using TConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationManager;
#endif

namespace NetLah.Extensions.Configuration;

public class AddFileConfigurationSource : IConfigurationSource
{
    private readonly AddFileConfigurationSourceOptionsBuilder _options;
    private readonly AddFileOptions _defaultOptions;
    private readonly IEnumerable<Action<AddFileContext>> _handlers;
    private string? _lastAddFileState;
    private IConfigurationRoot? _lastConfiguration;

    public AddFileConfigurationSource(AddFileConfigurationSourceOptionsBuilder options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.ConfigurationSection == null)
        {
#if !NETSTANDARD
#pragma warning disable CA2208 // incorrect string argument is passed to a parameterized constructor
#endif
            throw new ArgumentNullException(nameof(options.ConfigurationSection));
#if !NETSTANDARD
#pragma warning restore CA2208 // incorrect string argument is passed to a parameterized constructor
#endif
        }
        _defaultOptions = new AddFileOptions { Optional = true, ReloadOnChange = true };
        options.TryAddProvider(".json", JsonConfigurationExtensions.AddJsonFile);
        _handlers = new[] { DefaultAddFileByProvider }.Concat(_options.Handlers);
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var logger = Helper.GetLogger();
        var configurationSection = _options.ConfigurationSection;

        var settingLines = configurationSection.AsEnumerable()
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToArray();
        var addFileState = string.Join(Environment.NewLine, settingLines);
        var configuration = _lastConfiguration;

        if (configuration == null || !string.Equals(_lastAddFileState, addFileState))
        {
#if NETSTANDARD
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection();
#else
            var configurationBuilder = new TConfigurationBuilder();
#endif
            configurationBuilder.SetFileProvider(builder.GetFileProvider());
            var supportedExtensions = string.Join(", ", _options.ConfigureAddFiles.Keys.OrderBy(k => k));
            var configSections = configurationSection.GetChildren().ToArray();

            foreach (var configSection in configSections)
            {
                if (configSection.Value == null && configSection["Provider"] is { } typeValue1 && ("Settings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase) || "DefaultSettings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase)))
                {
                    configSection.Bind(_defaultOptions);
                    _defaultOptions.ResetCache();
                    logger.LogInformation("AddFile default settings {@settings}", _defaultOptions);
                }
            }

            foreach (var configSection in configSections)
            {
                var context = new AddFileContext
                {
                    Configuration = configSection,
                    ConfigurationBuilder = configurationBuilder,
                    Logger = logger,
                    SupportedExtensions = supportedExtensions,
                    IsProcessed = false,
                };

                var processed = false;

                if (configSection.Value is { } value1)
                {
                    var extensionOrProvider = Path.GetExtension(value1);
                    context.Provider = extensionOrProvider;
                    context.Source = new AddFileSource
                    {
                        LoggingLevel = _defaultOptions.LoggingLevel,
                        Optional = _defaultOptions.Optional,
                        ReloadOnChange = _defaultOptions.ReloadOnChange,
                        Path = value1,
                        OriginalPath = value1,
                    };
                }
                else if (configSection.Value == null && configSection["Provider"] is { } typeValue1 &&
                    ("Settings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase) || "DefaultSettings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase)))
                {
                    // Settings and type already processed
                    processed = true;
                }
                else
                {
                    var provider = configSection["Provider"];
                    var path = configSection["Path"];
                    var extensionOrProvider = provider ?? Path.GetExtension(path);
                    var addFileSource = new AddFileSource
                    {
                        LoggingLevel = _defaultOptions.LoggingLevel,
                        Optional = _defaultOptions.Optional,
                        ReloadOnChange = _defaultOptions.ReloadOnChange
                    };
                    configSection.Bind(addFileSource);
                    addFileSource.OriginalPath = addFileSource.Path;
                    context.Provider = extensionOrProvider;
                    context.Source = addFileSource;
                }

                if (!processed)
                {
                    foreach (var handler in _handlers)
                    {
                        handler(context);
                        if (context.IsProcessed)
                        {
                            processed = true;
                            break;
                        }
                    }
                }

                if (!processed)
                {
                    if (_defaultOptions.IsEnableLogging())
                    {
                        logger.LogError("AddFile unknown entry {@entry}", FormatConfigurationSection(configSection));
                    }

                    if (_defaultOptions.ThrowIfNotSupport ?? _options.ThrowIfNotSupport ?? false)
                    {
                        throw new Exception($"AddFile is not supported file extension/provider {context.Provider}, only support {context.SupportedExtensions} with file {context.Source.Path}");
                    }
                }
            }

#if NET7_0_OR_GREATER
            Dictionary<string, string?>?
#else
            Dictionary<string, string>?
#endif
            FormatConfigurationSection(IConfigurationSection configurationSection)
            {
                try
                {
                    return configurationSection.AsEnumerable()
                        .Where(kv => kv.Key != configurationSection.Path)
                        .ToDictionary(kv => kv.Key[(configurationSection.Path.Length + 1)..], kv => kv.Value);
                }
                catch
                {
                    return null;
                }
            }

#if NETSTANDARD
            configuration = configurationBuilder.Build();
#else
            configuration = configurationBuilder;
#endif

            _lastAddFileState = addFileState;
            _lastConfiguration = configuration;
        }
        else
        {
            logger.Log(_defaultOptions.GetLogLevel(), "AddFile configuration uses cache");
        }

        return new ChainedConfigurationProvider(new ChainedConfigurationSource
        {
            Configuration = configuration,
            ShouldDisposeConfiguration = false,
        });
    }

    private void DefaultAddFileByProvider(AddFileContext context)
    {
        var source = context.Source;
        if (context.Provider is { } provider
            && !string.IsNullOrWhiteSpace(provider)
            && source.OriginalPath is { } path
            && !string.IsNullOrWhiteSpace(path)
            && _options.ConfigureAddFiles.TryGetValue(provider, out var configureAddFile))
        {
            context.IsProcessed = true;
            if (configureAddFile.ResolveAbsolute)
            {
                source.Path = path = Path.GetFullPath(path);
            }
            context.Logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", path);
            configureAddFile.ConfigureAction(context.ConfigurationBuilder, source);
        }
    }
}
