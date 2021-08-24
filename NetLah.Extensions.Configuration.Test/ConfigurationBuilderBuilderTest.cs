using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration
{
    public class ConfigurationBuilderBuilderTest
    {
        private static string[] GetCommandLines() => new[] { "--CommandLineKey", "CommandLineValue1" };

        private static IEnumerable<KeyValuePair<string, string>> GetInMemrory() => new Dictionary<string, string>
        {
            ["Key1"] = "Value2",
            ["Key3:Sub4"] = "Value5",
        };

        private static void AssertProviders(IConfigurationRoot configuration, string[] providerNames)
        {
            Assert.NotNull(configuration);
            Assert.Equal(providerNames.Length, configuration.Providers.Count());
            Assert.Equal(providerNames, configuration
                .Providers
                .Select(p => p.GetType().Name)
                .ToArray());
        }

        private static void AssertProduction(IConfiguration configuration)
        {
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        }

        private static void AssertDevelopment(IConfiguration configuration)
        {
            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentDevelopmentValue1", configuration["EnvironmentKey"]);
        }

        private static void AssertCommandLines(IConfiguration configuration)
        {
            Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        }

        private static void AssertIni(IConfiguration configuration, bool success = true)
        {
            if (success)
            {
                Assert.Equal("ini Value2", configuration["IniKey1"]);
                Assert.Equal("Value6", configuration["IniSection3:Section4:Key5"]);
            }
            else
            {
                Assert.Null(configuration["IniKey1"]);
                Assert.Null(configuration["IniSection3:Section4:Key5"]);
            }
        }

        private static void AssertXml(IConfiguration configuration, bool success = true)
        {
            if (success)
            {
                Assert.Equal("xmlValue2", configuration["XmlKey1"]);
                Assert.Equal("Value5", configuration["XmlSection3:Key4"]);
            }
            else
            {
                Assert.Null(configuration["XmlKey1"]);
                Assert.Null(configuration["XmlSection3:Key4"]);
            }
        }

        private static void AssertInMemrory(IConfiguration configuration)
        {
            Assert.Equal("Value2", configuration["Key1"]);
            Assert.Equal("Value5", configuration["Key3:Sub4"]);
        }

        [Fact]
        public void Default_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertProduction(configuration);
        }

        [Fact]
        public void WithAddConfiguration_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithAddConfiguration(b => b.AddInMemoryCollection(GetInMemrory()))
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "MemoryConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertInMemrory(configuration);
        }

        [Fact]
        public void WithAppSecrets_Production_Success()
        {
            var builder = new ConfigurationBuilderBuilder()
                .WithAppSecrets<ConfigurationBuilderBuilderTest>();

            var configuration = builder.Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            Assert.Equal("Production", builder.EnvironmentName);
            AssertProduction(configuration);
        }

        [Fact]
        public void WithAppSecrets_Development_Success()
        {
            var builder = new ConfigurationBuilderBuilder()
                .WithEnvironment("Development")
                .WithAppSecrets<ConfigurationBuilderBuilderTest>();

            var configuration = builder.Build();

            AssertProviders(configuration, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            Assert.Equal("Development", builder.EnvironmentName);
            AssertDevelopment(configuration);
        }

        [Fact]
        public void WithBaseDirectory_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithBaseDirectory()
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertProduction(configuration);
        }

        [Fact]
        public void WithBasePath_Empty_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithBasePath("")
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertProduction(configuration);
        }

        [Fact]
        public void WithBasePath_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithBasePath("New-Location")
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            Assert.Equal("MainValue1-new-location", configuration["MainKey"]);
            Assert.Null(configuration["EnvironmentKey"]);
        }

        [Fact]
        public void WithCurrentDirectory_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithCurrentDirectory()
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertProduction(configuration);
        }

        [Fact]
        public void WithCommandLines_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithCommandLines("--Key1", "Value2a", "/Key3:Sub4", "Value5b")
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            Assert.Equal("Value2a", configuration["Key1"]);
            Assert.Equal("Value5b", configuration["Key3:Sub4"]);
            AssertProduction(configuration);
        }

        [Fact]
        public void WithCommandLines_Null()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithCommandLines(null)
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            });

            AssertProduction(configuration);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void WithCommandLines_Create_Null_Explicit()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(null)
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            });

            AssertProduction(configuration);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void WithCommandLines_Create_Null_Implicit()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            });

            AssertProduction(configuration);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void WithCommandLines_NullString()
        {
            var argsHasNull = new string[] { null };

            Assert.Throws<NullReferenceException>(() => new ConfigurationBuilderBuilder().WithCommandLines(argsHasNull).Build());
        }

        [Fact]
        public void WithConfiguration_Success()
        {
            var initialConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(GetInMemrory())
                .Build();

            var configuration = new ConfigurationBuilderBuilder()
                .WithConfiguration(initialConfiguration)
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertInMemrory(configuration);

            var configuration2 = new ConfigurationBuilderBuilder()
              .WithConfiguration(initialConfiguration.GetSection("Key3"))
              .Build();

            Assert.Null(configuration2["Key1"]);
            Assert.Null(configuration2["Key3:Sub4"]);
            Assert.Equal("Value5", configuration2["Sub4"]);
        }

        [Fact]
        public void WithInMemory_Success()
        {
            var configuration = new ConfigurationBuilderBuilder()
                .WithInMemory(GetInMemrory())
                .Build();

            AssertProviders(configuration, new[] {
                "MemoryConfigurationProvider",
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertInMemrory(configuration);
        }

        [Fact]
        public void Build_Production_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            AssertProduction(configuration);
            AssertCommandLines(configuration);
        }

        [Fact]
        public void Build_Production2_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .WithEnvironment("Production")
                .Build();

            AssertProviders(configuration, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            AssertProduction(configuration);
            AssertCommandLines(configuration);
        }

        [Fact]
        public void Build_Production_changeTo_dev_Success()
        {
            // add file 'appsettings.dev.json' to solve Ubuntu file name case-sensitive
            var builder = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .WithEnvironment("Production");

            Assert.NotNull(builder.Builder);
            Assert.Equal("Production", builder.EnvironmentName);
            var configuration1 = builder.Build();
            AssertProduction(configuration1);

            var configuration2 = builder.WithEnvironment("dev").Build();
            Assert.Equal("dev", builder.EnvironmentName);

            AssertProviders(configuration2, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            AssertDevelopment(configuration2);
            AssertCommandLines(configuration2);
        }

        [Fact]
        public void Build_Development_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .WithEnvironment("Development")
                .Build();

            AssertProviders(configuration, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            AssertDevelopment(configuration);
            AssertCommandLines(configuration);
        }

        [Fact]
        public void Build_Development2_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
                .WithEnvironment("Development")
                .Build();

            AssertProviders(configuration, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertDevelopment(configuration);
            Assert.Null(configuration["CommandLineKey"]);
        }

        [Fact]
        public void Build_Testing_Success()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .WithEnvironment("Testing")
                .Build();

            AssertProviders(configuration, new[] {
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            Assert.Equal("MainValue1", configuration["MainKey"]);
            Assert.Equal("EnvironmentTestingValue1", configuration["EnvironmentKey"]);
            AssertCommandLines(configuration);
        }

        [Fact]
        public void Build_Production_IniXml()
        {
            var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
                .WithAddConfiguration(
                    cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true)
                        .AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                )
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "XmlConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            });

            AssertProduction(configuration);
            AssertCommandLines(configuration);
            AssertXml(configuration);
            AssertIni(configuration);
        }

        [Fact]
        public void Build_Production_IniXml_WithAddConfiguration_Clear()
        {
            var builder = new ConfigurationBuilderBuilder()
                .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
                .WithAddConfiguration(cb => cb.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true));
            var configuration = builder.Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "XmlConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertProduction(configuration);
            Assert.Null(configuration["CommandLineKey"]);

            AssertIni(configuration);

            AssertXml(configuration);

            var configuration2 = builder.WithClearAddedConfiguration().Build();
            AssertProduction(configuration2);
            AssertIni(configuration2, false);
            AssertXml(configuration2, false);
        }

        [Fact]
        public void Build_FullApi()
        {
            var initConfig = new ConfigurationBuilder().Build();

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
                .WithInMemory(GetInMemrory())
                .WithConfiguration(initConfig)
                .Build();

            AssertProviders(configuration, new[] {
                "ChainedConfigurationProvider",
                "MemoryConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            });

            AssertInMemrory(configuration);

            AssertIni(configuration);
        }
    }
}
