using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class AddFileConfigurationSourceOptions
{
    public IConfigurationSection ConfigurationSection { get; set; } = null!;

    public bool? ThrowIfNotSupport { get; set; }
}
