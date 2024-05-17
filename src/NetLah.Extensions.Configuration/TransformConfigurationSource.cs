using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationSource(IConfigurationSection configurationSection) : IConfigurationSource
{
    private readonly IConfigurationSection _configurationSection = configurationSection;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TransformConfigurationProvider(_configurationSection);
    }
}
