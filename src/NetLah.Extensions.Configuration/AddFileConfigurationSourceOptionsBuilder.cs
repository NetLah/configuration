using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class AddFileConfigurationSourceOptionsBuilder : AddFileConfigurationSourceOptions
{
    public IConfigurationSection ConfigurationSection { get; set; } = null!;
}
