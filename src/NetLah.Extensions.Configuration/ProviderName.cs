namespace NetLah.Extensions.Configuration;

public class ProviderName
{
    public ProviderName(string? custom) : this(DbProviders.Custom, custom) { }

    public ProviderName(DbProviders provider, string? custom = null)
    {
        Provider = provider;
        if (provider == DbProviders.Custom && !string.IsNullOrWhiteSpace(custom))
        {
            Custom = custom;
        }
    }

    /// <summary>
    /// Providers: Custom, SQLServer, PostgreSQL, MySQL
    /// </summary>
    public DbProviders Provider { get; }

    /// <summary>
    /// Other provider name when Provider=Custom
    /// </summary>
    public string? Custom { get; }
}

internal class ProviderNameComparer : IEqualityComparer<ProviderName?>
{
    internal static readonly StringComparer DefaultStringComparer = StringComparer.OrdinalIgnoreCase;
    public static readonly ProviderNameComparer Instance = new();

    private ProviderNameComparer() { }

    public bool Equals(ProviderName? x, ProviderName? y)
    {
        if (x == null || y == null)
        {
            return x == null && y == null;
        }

        var result = x.Provider == y.Provider;
        var isNotCustom = x.Provider != DbProviders.Custom;

        if (!result || isNotCustom)
        {
            return result && isNotCustom;
        }

        var xNullOrEmpty = string.IsNullOrEmpty(x.Custom);
        var yNullOrEmpty = string.IsNullOrEmpty(y.Custom);
        return xNullOrEmpty || yNullOrEmpty ? xNullOrEmpty && yNullOrEmpty : DefaultStringComparer.Equals(x.Custom, y.Custom);
    }

    public int GetHashCode(ProviderName? obj)
    {
        if (obj != null)
        {
            var hash1 = obj.Provider.GetHashCode();
            return obj.Provider != DbProviders.Custom || string.IsNullOrEmpty(obj.Custom)
                ? hash1
                : hash1 ^ DefaultStringComparer.GetHashCode(obj.Custom);
        }
        return 0;
    }
}
