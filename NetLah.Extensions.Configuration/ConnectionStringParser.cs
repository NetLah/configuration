using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace NetLah.Extensions.Configuration;

internal class ConnectionStringParser
{
    internal static readonly Regex QuoteTokenRegex = new("(?<quote>([$%])\\1)|[$%]{(?<token>[a-zA-Z0-9_\\-\\s]{1,64})}|[$%]\\((?<token>[a-zA-Z0-9_\\-\\s]{1,64})\\)|[$%]\\[(?<token>[a-zA-Z0-9_\\-\\s]{1,64})\\]", RegexOptions.Compiled);
    private static readonly Regex NameAndProviderRegex = new("^(?<connectionName>.+)_(?<providerName>[^_]+)$", RegexOptions.Compiled);
    private static readonly StringComparer DefaultStringComparer = ProviderNameComparer.DefaultStringComparer;
    private static readonly StringComparison DefaultStringComparison = StringComparison.OrdinalIgnoreCase;

    private readonly IEnumerable<KeyValuePair<string, string>> _configuration;
    private readonly ProviderName? _selection;
    private readonly Func<string, string> _keyNormalizer;

    public ConnectionStringParser(IEnumerable<KeyValuePair<string, string>> configuration, ProviderName? providerName, Func<string, string> keyNormalizer)
    {
        _configuration = configuration;
        _selection = providerName;
        _keyNormalizer = keyNormalizer;
    }

    // https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.EnvironmentVariables/src/EnvironmentVariablesConfigurationProvider.cs#L15-L18
    internal static ProviderName ParseProviderName(string? providerName) => (providerName?.ToUpperInvariant()) switch
    {
        "MICROSOFT.DATA.SQLCLIENT" or "MSSQL" or "SQLAZURE" or "SQLSERVER" or "SYSTEM.DATA.SQLCLIENT" => new ProviderName(DbProviders.SQLServer),
        "MYSQL" or "MYSQLCONNECTOR" or "MYSQL.DATA.MYSQLCLIENT" => new ProviderName(DbProviders.MySQL),
        "NPGSQL" or "POSTGRES" or "POSTGRESQL" => new ProviderName(DbProviders.PostgreSQL),
        _ => new ProviderName(providerName),
    };

    internal static KeyValuePair<string, string>[] ParseConfigurationKeyValue(IConfiguration configuration)
        => configuration
            .GetChildren()
            .Where(s => s.Value != null)
            .Select(s => new KeyValuePair<string, string>(s.Key, s.Value!))
            .ToArray();

    public Dictionary<string, ProviderConnectionString> Parse()
    {
        var conns = ListConnections();
        var result = ExpandValue(conns);
        return result;
    }

    internal IEnumerable<ProviderConnectionString> ListConnections()
    {
        var lookup = _configuration
            .Select(c => (normalizedKey: _keyNormalizer(c.Key).ToUpperInvariant(), key: _keyNormalizer(c.Key), value: c.Value))
            .GroupBy(kv => kv.normalizedKey)
            .ToDictionary(g => g.Key, g => g.First(), DefaultStringComparer);

        var normalizedkeys = lookup.Keys.OrderBy(k => k.Length).ToArray();

        var selectedProvider = _selection?.Provider;
        var selectedCustom = _selection?.Custom;

        var conns = new List<ProviderConnectionString>();

        foreach (var key in normalizedkeys)
        {
            if (lookup.Remove(key, out var kv) && kv.value != null)
            {
                var m = NameAndProviderRegex.Match(kv.key);

                if (m.Success)
                {
                    var connectionName = m.Groups["connectionName"].Value;
                    var providerName = m.Groups["providerName"].Value;
                    var provider = Add(connectionName, providerName);

                    if (provider.Provider == DbProviders.Custom)
                    {
                        Add(kv.key, null);
                    }
                }
                else if (lookup.Remove($"{kv.key}_ProviderName", out var provider))   // parse Microsoft Azure style
                {
                    Add(kv.key, provider.value);
                }
                else
                {
                    Add(kv.key, null);
                }

                ProviderName Add(string connectionName, string? providerName)
                {
                    var provider = ParseProviderName(providerName);
                    var customProviderName = provider.Custom;
                    var isExactCustom = provider.Provider == DbProviders.Custom && selectedCustom != null;
                    var isSameCustom = string.Equals(selectedCustom, customProviderName, DefaultStringComparison);

                    if (_selection == null || (selectedProvider == provider.Provider && (!isExactCustom || isSameCustom)))
                    {
                        if (isExactCustom && isSameCustom)
                            customProviderName = selectedCustom;

                        conns.Add(new ProviderConnectionString(connectionName, kv.value, provider.Provider, customProviderName));
                    }

                    return provider;
                }
            }
        }

        return conns;
    }

    internal Dictionary<string, ProviderConnectionString> ExpandValue(IEnumerable<ProviderConnectionString> enumerable)
    {
        var list = enumerable.GroupBy(c => c.Name, DefaultStringComparer)
            .Select(group => NewEntry(group.First()))
            .ToList();

        var resolved = new Dictionary<string, Entry>(DefaultStringComparer);

        while (list.Count > 0)
        {
            foreach (var entry in list)
            {
                entry.Remaining = entry.Tokens.Except(resolved.Keys, DefaultStringComparer).Count();
            }

            var minRemaining = list.Min(e => e.Remaining);

            if (minRemaining == 0)
            {
                foreach (var entry in list.Where(e => e.Remaining == minRemaining).ToArray())
                {
                    Resolve(entry);
                }
            }
            else
            {
                var entry = list.First(i => i.Remaining == minRemaining);
                Resolve(entry);
            }

            void Resolve(Entry entry)
            {
                if (entry.HasQuotes || entry.Tokens.Any())
                {
                    entry.ConnStr.Value = QuoteTokenRegex.Replace(entry.Raw, ExpandMatchEvaluator);
                }
                list.Remove(entry);
                resolved.Add(entry.Name, entry);
            }

            string ExpandMatchEvaluator(Match match)
            {
                if (match.Groups["quote"].Value is { } quote && (quote == "$$" || quote == "%%"))
                {
                    return quote[..1];
                }

                if (match.Groups["token"].Value is { } token &&
                    !string.IsNullOrEmpty(token) && resolved.TryGetValue(_keyNormalizer(token), out var entry))
                {
                    return entry.ConnStr.Value;
                }

                return match.Value; // fallback if missing key or not resolved yet
            }
        }

        return resolved.ToDictionary(kv => kv.Key, kv => kv.Value.ConnStr, DefaultStringComparer);
    }

    internal Entry NewEntry(ProviderConnectionString connStr)
    {
        var raw = connStr.Raw;
        var matches = QuoteTokenRegex.Matches(raw);
        var hasQuotes = matches
               .Select(m => m.Groups["quote"].Value)
               .Any(quote => !string.IsNullOrEmpty(quote));
        var tokens = matches
                .Select(m => _keyNormalizer(m.Groups["token"].Value))
                .Where(token => !string.IsNullOrEmpty(token))
                .ToHashSet(DefaultStringComparer);

        return new Entry(connStr, hasQuotes, tokens);
    }

    internal class Entry
    {
        public Entry(ProviderConnectionString connStr, bool hasQuotes, HashSet<string> tokens)
        {
            ConnStr = connStr;
            HasQuotes = hasQuotes;
            Tokens = tokens;
        }

        public string Name => ConnStr.Name;    // for easier to debug and usage
        public string Raw => ConnStr.Raw;      // for easier to debug and usage
        public ProviderConnectionString ConnStr { get; }
        public bool HasQuotes { get; }
        public HashSet<string> Tokens { get; }
        public int Remaining { get; set; }
    }
}
