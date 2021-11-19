using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConnectionStringManagerTest
{
    private static readonly ProviderName? ProviderNameNull = null;
    private static readonly ProviderNameComparer DefaultProviderNameComparer = ProviderNameComparer.Instance;
    private readonly Mock<IDictionary<string, ProviderConnectionString>> _mockDict = new();
    private readonly Mock<Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName?, IDictionary<string, ProviderConnectionString>>> _mockFactory = new();

    private static IConnectionStringManager GetService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        return new ConnectionStringManager(configuration);
    }

    private IConnectionStringManager GetServiceMock(Func<string, string>? keyNormalizer)
    {
        var providerName = new ProviderName("Test");
        var configuration = Array.Empty<KeyValuePair<string, string>>();
        keyNormalizer ??= ConnectionStringsRoot.KeyTrimNormalizer;
        _mockFactory
            .Setup(s => s.Invoke(configuration,
                It.Is<Func<string, string>>(del => del == keyNormalizer),
                It.Is<ProviderName>(providerName, ProviderNameComparer.Instance)))
            .Returns(_mockDict.Object);
        var root = new ConnectionStringsRoot(configuration, keyNormalizer, _mockFactory.Object);

        return new ConnectionStringManager(root, providerName);
    }

    private (IConnectionStringManager, ProviderConnectionString, ProviderConnectionString) GetService2(Func<string, string>? keyNormalizer = null)
    {
        var service = GetServiceMock(keyNormalizer);
        var conn1 = new ProviderConnectionString("conn1", "connStr1");
        var conn2 = new ProviderConnectionString("conn2", "connStr2");
        SetupConnectionString("defaultConnection1", conn1);
        SetupConnectionString("defaultConnection2", conn2);
        return (service, conn1, conn2);
    }

    private void VerifyFactory(int times)
        => _mockFactory.Verify(
            s => s.Invoke(It.IsAny<KeyValuePair<string, string>[]>(),
                It.IsAny<Func<string, string>>(),
                It.IsAny<ProviderName>()
            ), Times.Exactly(times));

    private void SetupConnectionString(string key, ProviderConnectionString? outValue)
        => _mockDict.Setup(s => s.TryGetValue(key, out outValue)).Returns(true);

    private void VerifyTryGetValue(int times, string? key = null)
    {
        ProviderConnectionString? outValue;
        if (key == null)
        {
            _mockDict.Verify(s => s.TryGetValue(It.IsAny<string>(), out outValue), Times.Exactly(times));
        }
        else
        {
            _mockDict.Verify(s => s.TryGetValue(key, out outValue), Times.Exactly(times));
        }
    }

    [Fact]
    public void Ctor1()
    {
        var service = (ConnectionStringManager)GetService();
        var root = service.Root;
        Assert.NotNull(root);
        Assert.NotNull(root.Configuration);
        Assert.Equal(ProviderNameNull, service.Provider, DefaultProviderNameComparer);
        Assert.True(root.KeyNormalizer == ConnectionStringsRoot.KeyTrimNormalizer);
        Assert.False(root.KeyNormalizer == ConnectionStringsRoot.KeyPreserveSpaceNormalizer);
        Assert.True(root.Factory == ConnectionStringsRoot.ConnectionStringFactory);
    }

    [Fact]
    public void CloneWithKeyPreserveSpace_Test()
    {
        var service = GetService();
        var root = ((ConnectionStringManager)service).Root;
        var resul2 = (ConnectionStringManager)service.CloneWithKeyPreserveSpace();
        var root2 = resul2.Root;
        Assert.NotSame(root, root2);
        Assert.Same(root.Configuration, root2.Configuration);
        Assert.Equal(ProviderNameNull, resul2.Provider, DefaultProviderNameComparer);
        Assert.False(root2.KeyNormalizer == ConnectionStringsRoot.KeyTrimNormalizer);
        Assert.True(root2.KeyNormalizer == ConnectionStringsRoot.KeyPreserveSpaceNormalizer);
        Assert.True(root2.Factory == ConnectionStringsRoot.ConnectionStringFactory);

        var resul3 = (ConnectionStringManager)resul2.CloneWithKeyPreserveSpace();
        Assert.Same(resul2, resul3);
    }

    [Fact]
    public void CloneWithKeyPreserveSpace_SelectProvider_Test()
    {
        var service = GetService().CloneWithProvider("Cosmos");
        var root = ((ConnectionStringManager)service).Root;
        var resul5 = (ConnectionStringManager)service.CloneWithKeyPreserveSpace();
        var root5 = resul5.Root;
        Assert.NotSame(root, root5);
        Assert.Same(root.Configuration, root5.Configuration);
        Assert.Equal(new ProviderName("COSMOS"), resul5.Provider, DefaultProviderNameComparer);
        Assert.False(root5.KeyNormalizer == ConnectionStringsRoot.KeyTrimNormalizer);
        Assert.True(root5.KeyNormalizer == ConnectionStringsRoot.KeyPreserveSpaceNormalizer);
        Assert.True(root5.Factory == ConnectionStringsRoot.ConnectionStringFactory);

        var resul6 = (ConnectionStringManager)resul5.CloneWithKeyPreserveSpace();
        Assert.Same(resul5, resul6);
    }

    [Fact]
    public void CloneWithProvider_Null()
    {
        var service = GetService();
        var root = ((ConnectionStringManager)service).Root;
        var result = service.CloneWithProvider(null);
        Assert.Same(root, ((ConnectionStringManager)result).Root);
        Assert.Equal(ProviderNameNull, ((ConnectionStringManager)result).Provider, DefaultProviderNameComparer);
    }

    [Fact]
    public void CloneWithProvider_DbProviders()
    {
        var service = GetService();
        var root = ((ConnectionStringManager)service).Root;
        var result = service.CloneWithProvider(DbProviders.SQLServer);
        Assert.Same(root, ((ConnectionStringManager)result).Root);
        Assert.Equal(new ProviderName(DbProviders.SQLServer), ((ConnectionStringManager)result).Provider, DefaultProviderNameComparer);
    }

    [Fact]
    public void CloneWithProvider_String()
    {
        var service = GetService();
        var root = ((ConnectionStringManager)service).Root;
        var result = service.CloneWithProvider("postgresql");
        Assert.Same(root, ((ConnectionStringManager)result).Root);
        Assert.Equal(new ProviderName(DbProviders.PostgreSQL), ((ConnectionStringManager)result).Provider, DefaultProviderNameComparer);
    }

    [Fact]
    public void CloneWithProvider_Custom()
    {
        var service = GetService();
        var root = ((ConnectionStringManager)service).Root;
        var result = service.CloneWithProvider("cosmos");
        Assert.Same(root, ((ConnectionStringManager)result).Root);
        Assert.Equal(new ProviderName("COSMOS"), ((ConnectionStringManager)result).Provider, DefaultProviderNameComparer);
    }

    [Fact]
    public void CloneWithProvider_Exception()
    {
        var service = GetService();
        var result = Assert.ThrowsAny<InvalidOperationException>(() => service.CloneWithProvider(1));
        Assert.Equal("'selectingProvider' only supported type DbProviders or System.String (provided type 'System.Int32')", result.Message);
    }

    [Fact]
    public void Indexer_Initial()
    {
        _ = GetService2();
        VerifyFactory(0);
    }

    [Fact]
    public void Indexer_ConnectionStrings()
    {
        var (service, _, _) = GetService2();
        var connStrs = service.ConnectionStrings;
        Assert.Same(_mockDict.Object, connStrs);
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_ConnectionStrings_Twice()
    {
        var (service, _, _) = GetService2();
        var connStrs = service.ConnectionStrings;
        var connStrs2 = service.ConnectionStrings;
        Assert.Same(connStrs, connStrs2);
        Assert.Same(_mockDict.Object, connStrs2);
        VerifyFactory(1);
    }

    [Theory]
    [InlineData(0, 0, null)]
    [InlineData(1, 1, "")]
    [InlineData(1, 1, "defaultConnection")]
    public void Indexer_NullEmpty(int factoryTimes, int tryGetTimes, string connectionName)
    {
        var (service, _, _) = GetService2();
        var result = service[connectionName];
        Assert.Null(result);
        VerifyTryGetValue(tryGetTimes);
        VerifyFactory(factoryTimes);
    }

    [Theory]
    [InlineData(0, 0, null, null)]
    [InlineData(1, 1, null, "")]
    [InlineData(1, 2, null, "", "")]
    [InlineData(1, 1, null, null, "")]
    [InlineData(1, 2, "", null, "")]
    [InlineData(1, 2, "", null, "defaultConnection")]
    public void Indexer_NullEmpty2(int factoryTimes, int tryGetTimes, params string[] connectionNames)
    {
        var (service, _, _) = GetService2();
        var connectionName = connectionNames.First();
        connectionNames = connectionNames.Skip(1).ToArray();
        var result = service[connectionName, connectionNames];
        Assert.Null(result);
        VerifyTryGetValue(tryGetTimes);
        VerifyFactory(factoryTimes);
    }

    [Fact]
    public void Indexer_KeyNormalizer1()
    {
        var (service, _, _) = GetService2(ConnectionStringsRoot.KeyPreserveSpaceNormalizer);
        var result = service[" defaultConnection2  "];
        Assert.Null(result);
        VerifyTryGetValue(1);
        VerifyTryGetValue(0, "defaultConnection1");
        VerifyTryGetValue(0, "defaultConnection2");
        VerifyTryGetValue(1, " defaultConnection2  ");
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_KeyNormalizer2()
    {
        var (service, _, _) = GetService2(ConnectionStringsRoot.KeyPreserveSpaceNormalizer);
        var conn3 = new ProviderConnectionString("conn3", "connStr3");
        SetupConnectionString(" defaultConnection2  ", conn3);
        var result = service[" defaultConnection2  "];
        Assert.Same(conn3, result);
        VerifyTryGetValue(1);
        VerifyTryGetValue(0, "defaultConnection1");
        VerifyTryGetValue(0, "defaultConnection2");
        VerifyTryGetValue(1, " defaultConnection2  ");
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_Single1()
    {
        var (service, conn1, _) = GetService2();
        var result = service["defaultConnection1"];
        Assert.Same(conn1, result);
        VerifyTryGetValue(1);
        VerifyTryGetValue(1, "defaultConnection1");
        VerifyTryGetValue(0, "defaultConnection2");
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_Single2()
    {
        var (service, _, conn2) = GetService2();
        var result = service[" defaultConnection2  "];
        Assert.Same(conn2, result);
        VerifyTryGetValue(1);
        VerifyTryGetValue(0, "defaultConnection1");
        VerifyTryGetValue(1, "defaultConnection2");
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_Multi1()
    {
        var (service, conn1, _) = GetService2();
        var result = service[null, "", "not-exist1", "defaultConnection1", "not-call1"];
        Assert.Same(conn1, result);
        VerifyTryGetValue(3);
        VerifyTryGetValue(1, "defaultConnection1");
        VerifyTryGetValue(0, "defaultConnection2");
        VerifyTryGetValue(1, "");
        VerifyTryGetValue(1, "not-exist1");
        VerifyTryGetValue(0, "not-call1");
        VerifyFactory(1);
    }

    [Fact]
    public void Indexer_Multi2()
    {
        var (service, _, conn2) = GetService2();
        var result = service["not-exist1 ", " defaultConnection2  ", "defaultConnection1", "not-call1"];
        Assert.Same(conn2, result);
        VerifyTryGetValue(2);
        VerifyTryGetValue(0, "defaultConnection1");
        VerifyTryGetValue(1, "defaultConnection2");
        VerifyTryGetValue(0, "");
        VerifyTryGetValue(1, "not-exist1");
        VerifyTryGetValue(0, "not-call1");
        VerifyFactory(1);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("  ", null, null)]
    [InlineData(DbProviders.SQLServer, DbProviders.SQLServer, null)]
    [InlineData("sqlserver", DbProviders.SQLServer, null)]
    [InlineData("cosmos", DbProviders.Custom, "cosmos")]
    [InlineData(" Cosmos  ", DbProviders.Custom, " Cosmos  ")]
    public void ParseSelectProviderName_Test(object selectingProvider, DbProviders? expectedProvider, string expectedCustom)
    {
        var providerName = ConnectionStringManager.ParseSelectProviderName(selectingProvider);
        if (expectedProvider is { } expectedProviderValue)
        {
            Assert.NotNull(providerName);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expectedProviderValue, providerName.Provider);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expectedCustom, providerName.Custom);
        }
        else
        {
            Assert.Null(providerName);
        }
    }

    [Theory]
    [InlineData(1, "'selectingProvider' only supported type DbProviders or System.String (provided type 'System.Int32')")]
    [InlineData(false, "'selectingProvider' only supported type DbProviders or System.String (provided type 'System.Boolean')")]
    public void ParseSelectProviderName_Exception_Test(object selectingProvider, string expectedException)
    {
        var exception = Assert.ThrowsAny<InvalidOperationException>(() => ConnectionStringManager.ParseSelectProviderName(selectingProvider));
        Assert.Equal(expectedException, exception.Message);
    }
}
