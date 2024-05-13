using Microsoft.Extensions.Logging;

namespace NetLah.Extensions.Configuration;
public abstract class AddFileOptionsBase
{
    private LogLevel? _logLevel = default;

    public bool Optional { get; set; } = true;
    public bool ReloadOnChange { get; set; } = true;
    public string? LoggingLevel { get; set; }

    public bool IsEnableLogging() => GetLogLevel() != LogLevel.None;

    public LogLevel GetLogLevel()
    {
        return _logLevel ??= LoggingLevel?.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            "none" => LogLevel.None,
            _ => LogLevel.Information
        };
    }

    public void ResetCache() => _logLevel = default;
}

public class AddFileOptions : AddFileOptionsBase
{
    public bool? ThrowIfNotSupport { get; set; }
}
