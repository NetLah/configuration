using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NetLah.Extensions.Configuration.Test
{
    internal class Entry
    {
        public Entry(string name, ProviderConnectionString connStr)
        {
            Name = name;
            ConnStr = connStr;
        }
        public string Name { get; set; }
        public ProviderConnectionString ConnStr { get; set; }
    }

    internal class EntryComparer : IEqualityComparer<Entry>
    {
        private static readonly ProviderConnectionStringComparer ConnStrComparer = ProviderConnectionStringComparer.Instance;

        public static readonly EntryComparer Instance = new();

        public bool Equals([AllowNull] Entry x, [AllowNull] Entry y)
        {
            if (x == null || y == null)
                return x == null && y == null;

            return x.GetType() == y.GetType() &&
                string.Equals(x.Name, y.Name) &&
                x.ConnStr is ProviderConnectionString a &&
                y.ConnStr is ProviderConnectionString b &&
                ConnStrComparer.Equals(a, b);
        }

        public int GetHashCode([DisallowNull] Entry obj)
            => obj.Name.GetHashCode() ^ (obj.ConnStr is { } a ? ConnStrComparer.GetHashCode(a) : 0);
    }

    internal class ProviderConnectionStringComparer : IEqualityComparer<ProviderConnectionString>
    {
        public static readonly ProviderConnectionStringComparer Instance = new();
        private static readonly ProviderNameComparer DefaultProviderNameComparer = ProviderNameComparer.Instance;

        public bool Equals([AllowNull] ProviderConnectionString x, [AllowNull] ProviderConnectionString y)
        {
            if (x == null || y == null)
                return x == null && y == null;

            return string.Equals(x.Name, y.Name) &&
                string.Equals(x.Value, y.Value) &&
                DefaultProviderNameComparer.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ProviderConnectionString obj)
            => obj.Name.GetHashCode() ^
            (obj.Value?.GetHashCode() ?? 0) ^
            DefaultProviderNameComparer.GetHashCode(obj);
    }
}
