using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationProvider(IConfigurationSection configurationSection, IConfigurationRoot configurationRoot) : ConfigurationProvider
{
    private readonly IConfigurationSection _configurationSection = configurationSection;
    private readonly IConfigurationRoot _configurationRoot = configurationRoot;
    private object? _lock = null;
    private IChangeToken? _token;

    private void OnChange(object? obj)
    {
        Data.Clear();
        InternalLoad();
        OnReload();
    }

    public override void Load()
    {
        InternalLoad();

        if (Interlocked.CompareExchange(ref _lock, new object(), null) == null)
        {
            _token = _configurationSection.GetReloadToken();
            _token.RegisterChangeCallback(OnChange, this);
        }
    }

    private void InternalLoad()
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

                if (item["Ref"] is { } refValue && _configurationRoot[refValue] is { } refConfigValue1)
                {
                    TryParse(key + ":", refConfigValue1);
                }

                foreach (var section in item.GetSection("Value").GetChildren().Concat(item.GetSection("Values").GetChildren()))
                {
                    if (section.Value is { } sectionValue)
                    {
                        TryParse(key + ":", sectionValue);
                    }
                }

                foreach (var section in item.GetSection("Ref").GetChildren())
                {
                    if (section.Value is { } sectionValue && _configurationRoot[sectionValue] is { } refConfigValue2)
                    {
                        TryParse(key + ":", refConfigValue2);
                    }
                }
            }
        }

        void TryParse(string prefix, string keyValue)
        {
            var pos = keyValue.IndexOf('=');
            if (pos > 0 && pos < keyValue.Length - 1)
            {
                var key = keyValue[..pos];
                var value = keyValue[(pos + 1)..];
                Data[prefix + key] = value;
            }
        }
    }
}
