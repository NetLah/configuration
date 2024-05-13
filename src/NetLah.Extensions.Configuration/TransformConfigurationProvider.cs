using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationSection _configurationSection;

    public TransformConfigurationProvider(IConfigurationSection configurationSection)
    {
        _configurationSection = configurationSection;
    }

    public override void Load()
    {
        foreach (var item in _configurationSection.GetChildren())
        {
            if (item.Value is { } keyValue)
            {
                TryParse(string.Empty, keyValue);
            }
            else if (item["Key"] is { } key && !string.IsNullOrWhiteSpace(key))
            {
                if (item["Value"] is { } value)
                {
                    TryParse(key + ":", value);
                }
                else
                {
                    foreach (var section in item.GetSection("Values").GetChildren())
                    {
                        if (section.Value is { } sectionValue)
                        {
                            TryParse(key + ":", sectionValue);
                        }
                    }
                }
            }
        }
    }

    private void TryParse(string prefix, string keyValue)
    {
        var pos = keyValue.IndexOf('=');
        var key = keyValue[..pos];
        var value = keyValue[(pos + 1)..];
        Data[prefix + key] = value;
    }
}
