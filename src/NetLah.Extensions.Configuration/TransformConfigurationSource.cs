using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationSection _configurationSection;

    public TransformConfigurationSource(IConfigurationSection configurationSection)
    {
        _configurationSection = configurationSection;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TransformConfigurationProvider(_configurationSection);
    }
}
