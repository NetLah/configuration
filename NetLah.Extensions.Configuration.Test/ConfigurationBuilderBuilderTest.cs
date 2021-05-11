using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration
{
    public class ConfigurationBuilderBuilderTest
    {
        [Fact]
        public void Build_Production_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Production2_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .WithEnvironment("Production")
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Production_changeTo_Development_Success()
        {
            var builder = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .WithEnvironment("Production");

            Assert.NotNull(builder.Builder);
            Assert.Equal("Production", builder.EnvironmentName);
            var configuration1 = builder.Build();
            Assert.Equal("EnvironmentProductionValue1", configuration1["EnvironmentKey"]);

            var configuration2 = builder.WithEnvironment("development").Build();
            Assert.Equal("development", builder.EnvironmentName);

            Assert.NotNull(configuration2);
            Assert.Equal("MainValue1", configuration2["MainKey"]);
            Assert.Equal("EnvironmentDevelopmentValue1", configuration2["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration2["CommandLineKey"]);
        }

        [Fact]
        public void Build_Development_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .WithEnvironment("Development")
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentDevelopmentValue1", configuration["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Testing_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .WithEnvironment("Testing")
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentTestingValue1", configuration["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Null_Commandlines()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(null)
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_BaseDirectory()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithBaseDirectory()
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Production_IniXml()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(new[] { "--CommandLineKey", "CommandLineValue1" })
                .WithAddConfiguration(
                    cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true)
                        .AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                )
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);

            // xml
            Assert.Equal("xmlValue2", configuration["XmlKey1"]);
            Assert.Equal("Value5", configuration["XmlSection3:Key4"]);

            // ini
            Assert.Equal("ini Value2", configuration["IniKey1"]);
            Assert.Equal("Value6", configuration["IniSection3:Section4:Key5"]);
        }

        [Fact]
        public void Build_Production_IniXml_WithAddConfiguration()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(null)
                .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
                .WithAddConfiguration(cb => cb.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true))
                .Build();

            Assert.NotNull(configuration);
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
            Assert.Null(configuration["CommandLineKey"]);

            // xml
            Assert.Equal("xmlValue2", configuration["XmlKey1"]);
            Assert.Equal("Value5", configuration["XmlSection3:Key4"]);

            // ini
            Assert.Equal("ini Value2", configuration["IniKey1"]);
            Assert.Equal("Value6", configuration["IniSection3:Section4:Key5"]);
        }

        [Fact]
        public void Build_FullApi()
        {
            string[] args = null;
            var configuration = new ConfigurationBuilderBuilder()
                .WithBasePath("C:/App/bin")
                .WithCurrentDirectory()
                .WithBaseDirectory()
                .WithAppSecrets<ConfigurationBuilderBuilderTest>()
                .WithAppSecrets(typeof(ConfigurationBuilderBuilderTest).Assembly)
                .WithEnvironment("Staging")
                .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
                .WithCommandLines(args)
                .Build();

            Assert.NotNull(configuration);
        }
    }
}
