using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace NetLah.Extensions.Configuration;

public class TransformConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationSection _configurationSection;
    private object? _lock = null;
    private IChangeToken? _token;

    public TransformConfigurationProvider(IConfigurationSection configurationSection)
    {
        _configurationSection = configurationSection;
    }

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

        void TryParse(string prefix, string keyValue)
        {
            var pos = keyValue.IndexOf('=');
            var key = keyValue[..pos];
            var value = keyValue[(pos + 1)..];
            Data[prefix + key] = value;
        }
    }
}
