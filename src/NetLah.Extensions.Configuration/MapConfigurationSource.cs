using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class MapConfigurationSource(IConfiguration configuration, string sectionKey) : IConfigurationSource
{
    public IConfiguration Configuration { get; } = configuration;

    public string SectionKey { get; } = sectionKey;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new MapConfigurationProvider(this);
    }
}
