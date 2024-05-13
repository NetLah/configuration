namespace NetLah.Extensions.Configuration;

public class AddFileConfigurationSourceOptionsBuilder
{
    public AddFileConfigurationSourceOptionsBuilder(string sectionKey)
    {
        SectionKey = sectionKey;
    }

    public string SectionKey { get; }

    public bool? ThrowIfNotSupport { get; set; }
}
