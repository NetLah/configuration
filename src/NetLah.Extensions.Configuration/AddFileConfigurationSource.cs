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
    private readonly IConfigurationSection _configurationSection;
    private readonly bool? _throwIfNotSupport;
    private readonly AddFileOptions _defaultOptions;
    private string? _lastAddFileState;
    private IConfigurationRoot? _lastConfiguration;

    public AddFileConfigurationSource(IConfigurationSection configurationSection, bool? throwIfNotSupport = null)
    {
        _configurationSection = configurationSection ?? throw new ArgumentNullException(nameof(configurationSection));
        _throwIfNotSupport = throwIfNotSupport;
        _defaultOptions = new AddFileOptions { Optional = true, ReloadOnChange = true };
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var logger = Helper.GetLogger();

        var settingLines = _configurationSection.AsEnumerable()
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

            foreach (var item in _configurationSection.GetChildren())
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
                        try
                        {
                            logger.LogError("AddFile unknown type entry {@entry}", item.AsEnumerable().Where(kv => kv.Key != item.Path).ToDictionary(kv => kv.Key[(item.Path.Length + 1)..], kv => kv.Value));
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                }
            }

            foreach (var item in _configurationSection.GetChildren())
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
                        logger.LogError("AddFile unknown entry {@entry}", item);
                    }
                }
            }

            void AddFile(AddFileSource source)
            {
                var ext = Path.GetExtension(source.Path);
                if (".json".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", source.Path);
                    configurationBuilder.AddJsonFile(source.Path!, source.Optional, source.ReloadOnChange);
                }
                else if (".xml".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", source.Path);
                    configurationBuilder.AddXmlFile(source.Path!, source.Optional, source.ReloadOnChange);
                }
                else if (".ini".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Log(source.GetLogLevel(), "Add configuration file {filePath}", source.Path);
                    configurationBuilder.AddIniFile(source.Path!, source.Optional, source.ReloadOnChange);
                }
                else
                {
                    if (source.IsEnableLogging())
                    {
                        logger.LogError("AddFile is not supported file extension {extension} of file {sourceFile}", ext, source.Path);
                    }

                    if (_defaultOptions.ThrowIfNotSupport ?? _throwIfNotSupport ?? false)
                    {
                        throw new Exception($"AddFile is not supported file extension {ext}, only support .json, .ini and .xml. AddFile source {source.Path}");
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
