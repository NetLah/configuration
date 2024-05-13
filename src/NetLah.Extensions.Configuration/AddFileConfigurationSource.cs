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
                if (item.Value == null && item["Type"] is { } typeValue1)
                {
                    if ("Settings".Equals(typeValue1, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Bind(_defaultOptions);
                        _defaultOptions.ResetCache();
                        logger.LogInformation("AddFile default settings {@settings}", _defaultOptions);
                    }
                    else
                    {
                        logger.LogError("AddFile unknown type entry {@entry}", FormatConfigurationSection(item));
                    }
                }
            }

            foreach (var item in configurationSection.GetChildren())
            {
                if (item.Value is { } value1)
                {
                    AddFile(new AddFileSource
                    {
                        LoggingLevel = _defaultOptions.LoggingLevel,
                        Optional = _defaultOptions.Optional,
                        ReloadOnChange = _defaultOptions.ReloadOnChange,
                        Path = value1,
                    });
                }
                else if (item.Value == null && item["Type"] is { } typeValue1)
                {
                    // Settings and type already processed
                }
                else if (item["Path"] is { } path)
                {
                    var addFileSource = new AddFileSource
                    {
                        LoggingLevel = _defaultOptions.LoggingLevel,
                        Optional = _defaultOptions.Optional,
                        ReloadOnChange = _defaultOptions.ReloadOnChange
                    };
                    item.Bind(addFileSource);
                    addFileSource.ResetCache();
                    AddFile(addFileSource);
                }
                else
                {
                    if (_defaultOptions.IsEnableLogging())
                    {
                        logger.LogError("AddFile unknown entry {@entry}", FormatConfigurationSection(item));
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

            void AddFile(AddFileSource source)
            {
                var ext = Path.GetExtension(source.Path);
                if (_options.ConfigureAddFiles.TryGetValue(ext, out var configureAddFile))
                {
                    logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", source.Path);
                    configureAddFile(configurationBuilder, source);
                }
                else
                {
                    if (source.IsEnableLogging())
                    {
                        logger.LogError("AddFile is not supported file extension {extension}, only support {supportedExtensions} with {sourceFile}", ext, supportedExtensions, source.Path);
                    }

                    if (_defaultOptions.ThrowIfNotSupport ?? _options.ThrowIfNotSupport ?? false)
                    {
                        throw new Exception($"AddFile is not supported file extension {ext}, only support {supportedExtensions} with file {source.Path}");
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
