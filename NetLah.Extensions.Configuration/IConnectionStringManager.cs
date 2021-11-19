namespace NetLah.Extensions.Configuration;

public interface IConnectionStringManager
{
    IDictionary<string, ProviderConnectionString> ConnectionStrings { get; }

    ProviderConnectionString? this[string? connectionName, params string[]? connectionNames] { get; }

    IConnectionStringManager CloneWithProvider(object? selectedProvider);

    IConnectionStringManager CloneWithKeyPreserveSpace();
}
