namespace NetLah.Extensions.Configuration;

public class AddFileSource : AddFileOptionsBase
{
    public string Path { get; set; } = null!;

    public string? OriginalPath { get; set; } = null;
}
