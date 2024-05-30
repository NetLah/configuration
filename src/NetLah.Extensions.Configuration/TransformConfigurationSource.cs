using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationSource(IConfigurationSection configurationSection, IConfigurationRoot configurationRoot) : IConfigurationSource
{
    private readonly IConfigurationSection _configurationSection = configurationSection;
    private readonly IConfigurationRoot _configurationRoot = configurationRoot;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TransformConfigurationProvider(_configurationSection, _configurationRoot);
    }
}
