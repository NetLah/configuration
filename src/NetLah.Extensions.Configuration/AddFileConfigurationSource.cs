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
    private string? _lastAddFileState;
    private IConfigurationRoot? _lastConfiguration;

    public AddFileConfigurationSource(AddFileConfigurationSourceOptionsBuilder options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.ConfigurationSection == null)
        {
            throw new ArgumentNullException(nameof(options.ConfigurationSection));
        }
        _defaultOptions = new AddFileOptions { Optional = true, ReloadOnChange = true };
        options.TryAddProvider(".json", JsonConfigurationExtensions.AddJsonFile);
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

            foreach (var item in configurationSection.GetChildren())
            {
                if (item.Value == null && item["Provider"] is { } typeValue1 && ("Settings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase) || "DefaultSettings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Bind(_defaultOptions);
                    _defaultOptions.ResetCache();
                    logger.LogInformation("AddFile default settings {@settings}", _defaultOptions);
                }
            }

            foreach (var item in configurationSection.GetChildren())
            {
                if (item.Value is { } value1)
                {
                    var extensionOrType = Path.GetExtension(value1);
                    AddFile(extensionOrType, new AddFileSource
                    {
                        LoggingLevel = _defaultOptions.LoggingLevel,
                        Optional = _defaultOptions.Optional,
                        ReloadOnChange = _defaultOptions.ReloadOnChange,
                        Path = value1,
                    });
                }
                else if (item.Value == null && item["Provider"] is { } typeValue1 &&
                    ("Settings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase) || "DefaultSettings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase)))
                {
                    // Settings and type already processed
                }
                else
                {
                    var provider = item["Provider"];
                    var path = item["Path"];
                    var extensionOrProvider = provider ?? Path.GetExtension(path);
                    if (extensionOrProvider != null)
                    {
                        var addFileSource = new AddFileSource
                        {
                            LoggingLevel = _defaultOptions.LoggingLevel,
                            Optional = _defaultOptions.Optional,
                            ReloadOnChange = _defaultOptions.ReloadOnChange
                        };
                        item.Bind(addFileSource);
                        addFileSource.OriginalPath = path;
                        AddFile(extensionOrProvider, addFileSource);
                    }
                    else
                    {
                        if (_defaultOptions.IsEnableLogging())
                        {
                            logger.LogError("AddFile unknown entry {@entry}", FormatConfigurationSection(item));
                        }
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

            void AddFile(string extensionOrType, AddFileSource source)
            {
                if (_options.ConfigureAddFiles.TryGetValue(extensionOrType, out var configureAddFile))
                {
                    if (configureAddFile.ResolveAbsolute)
                    {
                        source.Path = Path.GetFullPath(source.Path);
                    }
                    logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", source.Path);
                    configureAddFile.ConfigureAction(configurationBuilder, source);
                }
                else
                {
                    if (source.IsEnableLogging())
                    {
                        logger.LogError("AddFile is not supported file extension/provider {extension}, only support {supportedExtensions} with {sourceFile}", extensionOrType, supportedExtensions, source.Path);
                    }

                    if (_defaultOptions.ThrowIfNotSupport ?? _options.ThrowIfNotSupport ?? false)
                    {
                        throw new Exception($"AddFile is not supported file extension/provider {extensionOrType}, only support {supportedExtensions} with file {source.Path}");
                    }
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
}
