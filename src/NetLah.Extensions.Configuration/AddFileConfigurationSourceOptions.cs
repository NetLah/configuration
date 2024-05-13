using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public delegate void ConfigureAddFileDelegate(IConfigurationBuilder builder, AddFileSource source);

public class AddFileConfigurationSourceOptions
{
    public IConfigurationSection ConfigurationSection { get; set; } = null!;

    public IDictionary<string, ConfigureAddFileDelegate> ConfigureAddFiles { get; } = new Dictionary<string, ConfigureAddFileDelegate>(StringComparer.OrdinalIgnoreCase);

    public bool? ThrowIfNotSupport { get; set; }
}
