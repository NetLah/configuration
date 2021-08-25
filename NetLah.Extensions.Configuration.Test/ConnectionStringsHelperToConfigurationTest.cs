using System;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration.Test
{
    public class ConnectionStringsHelperToConfigurationTest
    {
        private static IConfiguration GetService(string connectionString) => ConnectionStringsHelper.ToConfiguration(connectionString);

        private static T ConfigBind<T>(IConfiguration configuration) where T : new()
        {
            var result = new T();
            configuration.Bind(result);
            return result;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EmptyOrNullTest(string connectionString)
        {
            var service = (IConfigurationRoot)GetService(connectionString);

            Assert.Single(service.Providers);

            Assert.Empty(service.GetChildren());

            Assert.Null(service.Get<ServiceOptions>());

            Assert.NotNull(ConfigBind<ServiceOptions>(service));
        }

        private static void AssertParital1(ServiceOptions svcOptions)
        {
            Assert.NotNull(svcOptions);
            Assert.Equal("https://7d48.documents.azure.com:443/", svcOptions.AccountEndpoint);
            Assert.Null(svcOptions.AccountKey);
        }

        private static void AssertParital2(CustomCosmosOptions cosmosOptions)
        {
            Assert.NotNull(cosmosOptions);
            Assert.Equal(new Uri("https://7d48.documents.azure.com:443/"), cosmosOptions.AccountEndpoint);
            Assert.Null(cosmosOptions.AccountKey);
        }

        [Fact]
        public void ConfigGetPartialTest()
        {
            var service = GetService("accountEndpoint=https://7d48.documents.azure.com:443/;");
            AssertParital1(service.Get<ServiceOptions>());
            AssertParital2(service.Get<CustomCosmosOptions>());
        }

        [Fact]
        public void ConfigBindPartialTest()
        {
            var service = GetService("accountEndpoint=https://7d48.documents.azure.com:443/;");
            AssertParital1(ConfigBind<ServiceOptions>(service));
            AssertParital2(ConfigBind<CustomCosmosOptions>(service));
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

        [Fact]
        public void ConfigGetFullTest()
        {
            var service = GetService(" provider = \" The Name \" ; accountEndpoint = ; AccountKey= Pa$$w0rd ; Number = -1234 ; AMOUNT = 3.14159");

            var svcOptions = service.Get<ServiceOptions>();

            AssertFull(service, svcOptions);
        }


        [Fact]
        public void ConfigBindFullTest()
        {
            var service = GetService(" provider = \" The Name \" ; accountEndpoint = ; AccountKey= Pa$$w0rd ; Number = -1234 ; AMOUNT = 3.14159");

            var svcOptions = ConfigBind<ServiceOptions>(service);

            AssertFull(service, svcOptions);
        }

        private static void AssertOther(IConfiguration service, ServiceOptions svcOptions)
        {
            Assert.Equal("", service["AccountEndpoint"]);

            Assert.NotNull(svcOptions);
            Assert.Equal("", svcOptions.AccountEndpoint);
            Assert.Equal(TimeSpan.Parse("1.23:05:00"), svcOptions.Duration);
            Assert.Equal(new Uri("file:///C:/Temp/Document.pdf"), svcOptions.Url);
        }

        [Fact]
        public void ConfigGetOhterTest()
        {
            var service = GetService(" accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

            var svcOptions = service.Get<ServiceOptions>();

            AssertOther(service, svcOptions);
        }

        [Fact]
        public void ConfigBinOhterTest()
        {
            var service = GetService(" accountEndpoint = \"\" ; duration= 1.23:5 ; url = file:///C:/Temp/Document.pdf ");

            var svcOptions = ConfigBind<ServiceOptions>(service);

            AssertOther(service, svcOptions);
        }

#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        private class ServiceOptions
        {
            public string Provider { get; set; }
            public string AccountEndpoint { get; set; }
            public string AccountKey { get; set; }
            public string Allow { get; set; }
            public int Number { get; set; }
            public decimal Amount { get; set; }
            public TimeSpan Duration { get; set; }
            public Uri Url { get; set; }
        }

        private class CustomCosmosOptions
        {
            public Uri AccountEndpoint { get; set; }
            public string AccountKey { get; set; }
        }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
