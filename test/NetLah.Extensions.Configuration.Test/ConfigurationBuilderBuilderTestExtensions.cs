using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration.Test;

internal static class ConfigurationBuilderBuilderTestExtensions
{
    public static IConfigurationRoot BuildConfigurationRoot(this ConfigurationBuilderBuilder builder)
    {
#if NET6_0_OR_GREATER
        return builder.Manager;
#else
        return builder.Build();
#endif
    }
}
