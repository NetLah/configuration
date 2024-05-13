using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public delegate void ConfigureAddFileDelegate(IConfigurationBuilder builder, AddFileSource source);

public class AddFileConfigurationSourceOptions
{
    public IDictionary<string, ConfigureAddFileDelegate> ConfigureAddFiles { get; } = new Dictionary<string, ConfigureAddFileDelegate>(StringComparer.OrdinalIgnoreCase);
    public string SectionKey { get; set; } = string.Empty;
    public bool? ThrowIfNotSupport { get; set; }
}
