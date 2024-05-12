using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#if NETSTANDARD
using TConfigurationBuilder = Microsoft.Extensions.Configuration.IConfigurationBuilder;
#else
using TConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationManager;
#endif

namespace NetLah.Extensions.Configuration;

public class ConfigurationSourceConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationSection _configurationSection;
    private readonly bool _throwIfNotSupport;
    private string? _lastConfigurationSourceState;
    private IConfigurationRoot? _lastConfiguration;

    public ConfigurationSourceConfigurationSource(IConfigurationSection configurationSection, bool throwIfNotSupport = false)
    {
        _configurationSection = configurationSection ?? throw new ArgumentNullException(nameof(configurationSection));
        _throwIfNotSupport = throwIfNotSupport;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var logger = Helper.GetLogger();
        var settingLines = _configurationSection.AsEnumerable()
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToArray();
        var configurationSourceState = string.Join(Environment.NewLine, settingLines);
        var configuration = _lastConfiguration;
        if (configuration == null || !string.Equals(_lastConfigurationSourceState, configurationSourceState))
        {

            var sourceFiles = _configurationSection?.Get<string[]>() ?? Array.Empty<string>();

#if NETSTANDARD
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection();
#else
            var configurationBuilder = new TConfigurationBuilder();
#endif
            configurationBuilder.SetFileProvider(builder.GetFileProvider());

            foreach (var sourceFile in sourceFiles)
            {
                var ext = Path.GetExtension(sourceFile);
                if (".json".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Add configuration source {sourceFile}", sourceFile);
                    configurationBuilder.AddJsonFile(sourceFile, true, true);
                }
                else if (".xml".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Add configuration source {sourceFile}", sourceFile);
                    configurationBuilder.AddXmlFile(sourceFile, true, true);
                }
                else if (".ini".Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Add configuration source {sourceFile}", sourceFile);
                    configurationBuilder.AddIniFile(sourceFile, true, true);
                }
                else
                {
                    logger.LogError("ConfigurationSource is not supported file extension {extension} of file {sourceFile}", ext, sourceFile);

                    if (_throwIfNotSupport)
                    {
                        throw new Exception($"ConfigurationSource is not supported file extension {ext}, only support .json, .ini and .xml. ConfigurationSource file {sourceFile}");
                    }
                }
            }

#if NETSTANDARD
            configuration = configurationBuilder.Build();
#else
            configuration = configurationBuilder;
#endif

            _lastConfigurationSourceState = configurationSourceState;
            _lastConfiguration = configuration;
        }
        else
        {
            logger.LogInformation("ConfigurationSource uses cache");
        }

        return new ChainedConfigurationProvider(new ChainedConfigurationSource
        {
            Configuration = configuration,
            ShouldDisposeConfiguration = false,
        });
    }
}
