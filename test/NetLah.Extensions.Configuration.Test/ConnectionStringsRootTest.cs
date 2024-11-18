using Moq;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConnectionStringsRootTest
{
    [Fact]
    public void GetOrAddTest()
    {
        var mockFactory = new Mock<Func<KeyValuePair<string, string>[], Func<string, string>, ProviderName?, Dictionary<string, ProviderConnectionString>>>();
        var expectedNull1 = new Dictionary<string, ProviderConnectionString>();
        var expectedSqlServer2 = new Dictionary<string, ProviderConnectionString>();
        var expectedCosmos3 = new Dictionary<string, ProviderConnectionString>();
        SetupMock(null, expectedNull1);
        SetupMock(new ProviderName(DbProviders.SQLServer), expectedSqlServer2);
        SetupMock(new ProviderName("Cosmos"), expectedCosmos3);

        var service = new ConnectionStringsRoot(Array.Empty<KeyValuePair<string, string>>(), null, factory: mockFactory.Object);
        Assert.Empty(service.Cache);
        Verify(0);

        var result1a = service[null];
        Assert.Same(expectedNull1, result1a);
        Assert.Empty(service.Cache);
        Verify(1);

        // call with null again
        var result1b = service[null];
        Assert.Same(expectedNull1, result1b);
        Assert.Empty(service.Cache);
        Verify(1);

        // continue test other providerName
        var result2a = service[new ProviderName(DbProviders.SQLServer)];
        Assert.Same(expectedSqlServer2, result2a);
        Assert.Single(service.Cache);
        Verify(2);

        var result3a = service[new ProviderName("cosMOS")];
        Assert.Same(expectedCosmos3, result3a);
        Assert.Equal(2, service.Cache.Count);
        Verify(3);

        // call again
        var result1c = service[null];
        Assert.Same(expectedNull1, result1c);
        Assert.Equal(2, service.Cache.Count);
        Verify(3);

        var result2b = service[new ProviderName(DbProviders.SQLServer)];
        Assert.Same(expectedSqlServer2, result2b);
        Assert.Equal(2, service.Cache.Count);
        Verify(3);

        var result3b = service[new ProviderName("COSmos")];
        Assert.Same(expectedCosmos3, result3b);
        Assert.Equal(2, service.Cache.Count);
        Verify(3);

        void SetupMock(ProviderName? input, Dictionary<string, ProviderConnectionString> connStr)
        {
            mockFactory
                .Setup(s => s.Invoke(It.IsAny<KeyValuePair<string, string>[]>(),
                    It.IsAny<Func<string, string>>(),
                    It.Is(input, ProviderNameComparer.Instance)))
                .Returns(connStr);
        }

        void Verify(int times)
        {
            mockFactory.Verify(
                s => s.Invoke(It.IsAny<KeyValuePair<string, string>[]>(),
                    It.IsAny<Func<string, string>>(),
                    It.IsAny<ProviderName>()
                ), Times.Exactly(times));
        }
    }
}
