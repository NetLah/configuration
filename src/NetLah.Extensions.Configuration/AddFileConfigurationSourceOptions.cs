using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public delegate void ConfigureAddFileDelegate(IConfigurationBuilder builder, AddFileSource source);

public interface IConfigureAddFiles
{
    IDictionary<string, ConfigureAddFileOptions> ConfigureAddFiles { get; }

    List<Action<AddFileContext>> Handlers { get; }
}

public class ConfigureAddFileOptions
{
    public ConfigureAddFileDelegate ConfigureAction { get; set; } = null!;

    public bool ResolveAbsolute { get; set; }
}

public class AddFileConfigurationSourceOptions : IConfigureAddFiles
{
    public IDictionary<string, ConfigureAddFileOptions> ConfigureAddFiles { get; } = new Dictionary<string, ConfigureAddFileOptions>(StringComparer.OrdinalIgnoreCase);

    public List<Action<AddFileContext>> Handlers { get; } = [];

    public string SectionKey { get; set; } = string.Empty;

    public bool? ThrowIfNotSupport { get; set; }
}
