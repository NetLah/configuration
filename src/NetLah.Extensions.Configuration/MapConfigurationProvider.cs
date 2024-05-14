using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace NetLah.Extensions.Configuration;

public class MapConfigurationProvider(MapConfigurationSource source) : ConfigurationProvider
{
    private readonly MapConfigurationSource _source = source;
    private object? _lock = null;
    private IChangeToken? _token;
    private IConfigurationSection? _configurationSection;

    private void OnChange(object? obj)
    {
        Data.Clear();
        InternalLoad();
        OnReload();
    }

    public override void Load()
    {
        InternalLoad();

        if (_configurationSection != null && Interlocked.CompareExchange(ref _lock, new object(), null) == null)
        {
            _token = _configurationSection.GetReloadToken();
            _token.RegisterChangeCallback(OnChange, this);
        }
    }

    private void InternalLoad()
    {
        var configuration = _source.Configuration;
        _configurationSection ??= configuration.GetSection(_source.SectionKey);

        foreach (var item in _configurationSection.GetChildren())
        {
            if (item.Value is { } keyValue)
            {
                TryParse(keyValue);
            }
            else
            {
                var key1 = item["From"] ?? item["Source"];
                var key2 = item["To"] ?? item["Destination"] ?? item["Dest"];
                if (!string.IsNullOrEmpty(key1) && !string.IsNullOrEmpty(key2) && configuration[key1] is { } value)
                {
                    Data[key2] = value;
                }
            }
        }

        void TryParse(string keyValue)
        {
            var pos = keyValue.IndexOf('=');
            var key1 = keyValue[..pos];
            var key2 = keyValue[(pos + 1)..];
            if (configuration[key1] is { } value)
            {
                Data[key2] = value;
            }
        }
    }
}
