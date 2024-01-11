using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConnectionStringUtilitiesTest
{
    private static IConfiguration GetConfigurationFromString(string connectionString)
    {
        return ConnectionStringUtilities.ToConfiguration(connectionString);
    }

    private static T ConfigBind<T>(IConfiguration configuration) where T : new()
    {
        var result = new T();
        configuration.Bind(result);
        return result;
    }

    private static void AssertFull(IConfiguration service, ServiceOptions svcOptions)
    {
        Assert.Null(service["AccountEndpoint"]);

        Assert.NotNull(svcOptions);
        Assert.Equal(" The Name ", svcOptions.Provider);
        Assert.Null(svcOptions.AccountEndpoint);
        Assert.Equal("Pa$$w0rd", svcOptions.AccountKey);
        Assert.Null(svcOptions.Allow);
        Assert.Equal(-1234, svcOptions.Number);
        Assert.Equal(3.14159m, svcOptions.Amount);
        Assert.Equal(TimeSpan.Zero, svcOptions.Duration);
        Assert.Null(svcOptions.Url);
    }

    private static void AssertPartial1(ServiceOptions svcOptions)
    {
        Assert.NotNull(svcOptions);
        Assert.Equal("https://7d48.documents.azure.com:443/", svcOptions.AccountEndpoint);
        Assert.Null(svcOptions.AccountKey);
    }

    private static void AssertPartial2(CustomCosmosOptions cosmosOptions)
    {
        Assert.NotNull(cosmosOptions);
        Assert.Equal(new Uri("https://7d48.documents.azure.com:443/"), cosmosOptions.AccountEndpoint);
        Assert.Null(cosmosOptions.AccountKey);
    }

    private static void AssertOther(IConfiguration service, ServiceOptions svcOptions)
    {
        Assert.Equal("", service["AccountEndpoint"]);
        AssertOtherOptions(svcOptions);
    }

    private static void AssertOtherOptions(ServiceOptions svcOptions)
    {
        Assert.NotNull(svcOptions);
        Assert.Equal("", svcOptions.AccountEndpoint);
        Assert.Equal(TimeSpan.Parse("1.23:05:00"), svcOptions.Duration);
        Assert.Equal(new Uri("file:///C:/Temp/Document.pdf"), svcOptions.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyOrNullTest(string? connectionString)
    {
        var service = (IConfigurationRoot)GetConfigurationFromString(connectionString!);

        Assert.Single(service.Providers);

        Assert.Empty(service.GetChildren());

        Assert.Null(service.Get<ServiceOptions>());

        Assert.NotNull(ConfigBind<ServiceOptions>(service));
    }

    [Fact]
    public void ConfigGetPartialTest()
    {
        var service = GetConfigurationFromString("accountEndpoint=https://7d48.documents.azure.com:443/;");
        AssertPartial1(service.Get<ServiceOptions>()!);
        AssertPartial2(service.Get<CustomCosmosOptions>()!);
    }

    [Fact]
    public void ConfigBindPartialTest()
    {
        var service = GetConfigurationFromString("accountEndpoint=https://7d48.documents.azure.com:443/;");
        AssertPartial1(ConfigBind<ServiceOptions>(service));
        AssertPartial2(ConfigBind<CustomCosmosOptions>(service));
    }

    [Fact]
    public void ConfigGetFullTest()
    {
        var service = GetConfigurationFromString(" provider = \" The Name \" ; accountEndpoint = ; AccountKey= Pa$$w0rd ; Number = -1234 ; AMOUNT = 3.14159");

        var svcOptions = service.Get<ServiceOptions>()!;

        AssertFull(service, svcOptions);
    }

    [Fact]
    public void ConfigBindFullTest()
    {
        var service = GetConfigurationFromString(" provider = \" The Name \" ; accountEndpoint = ; AccountKey= Pa$$w0rd ; Number = -1234 ; AMOUNT = 3.14159");

        var svcOptions = ConfigBind<ServiceOptions>(service);

        AssertFull(service, svcOptions);
    }

    [Fact]
    public void ConfigGetOtherTest()
    {
        var service = GetConfigurationFromString(" accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

        var svcOptions = service.Get<ServiceOptions>()!;

        AssertOther(service, svcOptions);
    }

    [Fact]
    public void ConfigBindOtherTest()
    {
        var service = GetConfigurationFromString(" accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

        var svcOptions = ConfigBind<ServiceOptions>(service);

        AssertOther(service, svcOptions);
    }

    [Fact]
    public void ProviderConnectionStringToConfigurationTest()
    {
        var connStr = new ProviderConnectionString("dummy1", " accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

        Assert.Null(connStr.Configuration);

        var configuration = connStr.ToConfiguration();

        var svcOptions = configuration.Get<ServiceOptions>()!;

        Assert.NotNull(connStr.Configuration);
        AssertOtherOptions(svcOptions);
    }

    [Fact]
    public void ConnectionStringGetOptionsTest()
    {
        var connStr = " accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ";

        var svcOptions = ConnectionStringUtilities.Get<ServiceOptions>(connStr)!;

        AssertOtherOptions(svcOptions);
    }

    [Fact]
    public void ProviderConnectionStringGetOptionsTest()
    {
        var connStr = new ProviderConnectionString("dummy2", " accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

        Assert.Null(connStr.Configuration);

        var svcOptions = connStr.Get<ServiceOptions>()!;

        Assert.NotNull(connStr.Configuration);
        AssertOtherOptions(svcOptions);
    }

    [Fact]
    public void ConnectionStringBindOptionsTest()
    {
        var connStr = " accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ";

        var svcOptions = new ServiceOptions();
        var instance = ConnectionStringUtilities.Bind(connStr, svcOptions);

        Assert.Same(svcOptions, instance);
        AssertOtherOptions(svcOptions);
    }

    [Fact]
    public void ProviderConnectionStringBindOptionsTest()
    {
        var connStr = new ProviderConnectionString("dummy3", " accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

        Assert.Null(connStr.Configuration);

        var svcOptions = new ServiceOptions();
        var instance = connStr.Bind(svcOptions);

        Assert.Same(svcOptions, instance);
        Assert.NotNull(connStr.Configuration);
        AssertOtherOptions(svcOptions);
    }

    private class ServiceOptions
    {
        public string? Provider { get; set; }
        public string? AccountEndpoint { get; set; }
        public string? AccountKey { get; set; }
        public string? Allow { get; set; }
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public TimeSpan Duration { get; set; }
        public Uri? Url { get; set; }
    }

    private class CustomCosmosOptions
    {
        public Uri? AccountEndpoint { get; set; }
        public string? AccountKey { get; set; }
    }
}
