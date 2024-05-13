using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class AddFileConfigurationSourceOptions
{
    public AddFileConfigurationSourceOptions(IConfigurationSection configurationSection)
    {
        ConfigurationSection = configurationSection;
    }

    public IConfigurationSection ConfigurationSection { get; set; }

    public bool? ThrowIfNotSupport { get; set; }
}
