using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetLah.Extensions.Configuration;

public class AddFileSource : AddFileOptionsBase
{
    public string Path { get; set; } = null!;

    public string? OriginalPath { get; set; } = null;
}

public class AddFileContext
{
    public IConfigurationSection Configuration { get; set; } = null!;

    public IConfigurationBuilder ConfigurationBuilder { get; set; } = null!;

    public ILogger Logger { get; set; } = null!;

    public string SupportedExtensions { get; set; } = null!;

    public string? Provider { get; set; }

    public AddFileSource Source { get; set; } = null!;

    public bool IsProcessed { get; set; }
}
