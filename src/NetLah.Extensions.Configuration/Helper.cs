using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetLah.Extensions.Logging;

namespace NetLah.Extensions.Configuration;

internal static class Helper
{
    private static readonly Lazy<ILogger?> _loggerLazy = new(() => AppLogReference.GetAppLogLogger(typeof(ConfigurationSourceConfigurationSource).Namespace));

    public static ILogger GetLogger()
    {
        return _loggerLazy.Value ?? NullLogger.Instance;
    }
}
