using Microsoft.Extensions.Configuration;

namespace NetLah.Extensions.Configuration;

/// <summary>
/// Provider and connection string
/// </summary>
public sealed class ProviderConnectionString(string name, string connectionString, DbProviders provider = DbProviders.Custom, string? custom = null) : ProviderName(provider, custom)
{
    private string? _expanded;

    /// <summary>
    /// Connection string name
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Expanded connection string value
    /// </summary>
    public string Value { get => _expanded ?? Raw; set => _expanded = value; }

    /// <summary>
    /// Raw connection string value
    /// </summary>
    public string Raw { get; } = connectionString;

    public IConfiguration? Configuration { get; internal set; }
}
