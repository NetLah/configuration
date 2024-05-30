using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConfigurationBuilderBuilderTest
{
    private static string[] GetCommandLines() => ["--CommandLineKey", "CommandLineValue1", "/arg2", "value2b", "--arg3=value3c", "/arg4=value4d", "--Key5:Sub6", "value7e"];

    private static Dictionary<string, string?> GetInMemory() => new()
    {
        ["Key1"] = "Value2",
        ["Key3:Sub4"] = "Value5",
    };

    private static ConfigurationBuilder CreateDefaultBuilder(string[]? args = null, string? environmentName = null, Assembly? assembly = null, Type? type = null, string? basePath = null)
    {
        environmentName ??= Environments.Production;
        assembly ??= type?.Assembly;

        var builder = new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(basePath))
        {
            builder.SetBasePath(basePath);
        }

        builder.AddInMemoryCollection()
            .AddEnvironmentVariables(prefix: "DOTNET_")
            .AddEnvironmentVariables(prefix: "ASPNETCORE_")
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

        if (assembly != null && string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            builder.AddUserSecrets(assembly, optional: true, reloadOnChange: true);
        }

        builder.AddEnvironmentVariables();

        if (args is { Length: > 0 } args1)
        {
            builder.AddCommandLine(args1);
        }

        return builder;
    }

    private static void AssertProviders(IConfigurationRoot configuration, string[] providerNames, string?[]? extra = null)
    {
        providerNames =
        [
            .. (new string[] { "MemoryConfigurationProvider", "EnvironmentVariablesConfigurationProvider", "EnvironmentVariablesConfigurationProvider" }),
            .. providerNames,
        ];
        if (extra != null)
        {
            extra = [.. (new string?[] { null, null, null }), .. extra];
        }
        Assert.NotNull(configuration);
        var providers = configuration.Providers.ToArray();
        Assert.Equal(providerNames.Length, providers.Length);
        Assert.Equal(providerNames, providers
            .Select(p => p.GetType().Name)
            .ToArray());

        if (extra != null)
        {
            Assert.Equal(extra.Length, providers.Length);
            for (var i = 0; i < extra.Length; i++)
            {
                var provider = providers[i];
                if (extra[i] is { } path)
                {
                    if (provider is JsonConfigurationProvider jsonConfigurationProvider)
                    {
                        Assert.Equal(path, jsonConfigurationProvider.Source.Path);
                    }
                    else if (provider is IniConfigurationProvider iniConfigurationProvider)
                    {
                        Assert.Equal(path, iniConfigurationProvider.Source.Path);
                    }
                    else if (provider is XmlConfigurationProvider xmlConfigurationProvider)
                    {
                        Assert.Equal(path, xmlConfigurationProvider.Source.Path);
                    }
                }
            }
        }
    }

    private static void AssertProduction(IConfiguration configuration)
    {
        Assert.Equal("MainValue1", configuration["MainKey"]);
        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("ProductionSubValue1", configuration["ProductionSection:SubKey"]);
        Assert.Null(configuration["SecondaryKey"]);
    }

    private static void AssertDevelopment(IConfiguration configuration)
    {
        Assert.Equal("MainValue1", configuration["MainKey"]);
        Assert.Equal("EnvironmentDevelopmentValue1", configuration["EnvironmentKey"]);
        Assert.Equal("DevelopmentSubValue1", configuration["DevelopmentSection:SubKey"]);
    }

    private static void AssertCommandLines(IConfiguration configuration)
    {
        Assert.Equal("CommandLineValue1", configuration["CommandLineKey"]);
        Assert.Equal("value2b", configuration["arg2"]);
        Assert.Equal("value3c", configuration["arg3"]);
        Assert.Equal("value4d", configuration["arg4"]);
        Assert.NotNull(configuration.GetSection("no-key"));
        var section = configuration.GetSection("Key5");
        Assert.Null(section.Value);
        Assert.Equal("value7e", section["Sub6"]);
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

    private static void AssertInMemory(IConfiguration configuration)
    {
        Assert.Equal("Value2", configuration["Key1"]);
        Assert.Equal("Value5", configuration["Key3:Sub4"]);
    }

    private static void AssertTransform(IConfiguration configuration)
    {
        Assert.Equal("Information", configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Authentication"]);
        Assert.Equal("Error", configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Mvc"]);
        Assert.Equal("Verbose", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime"]);
        Assert.Equal("Warning", configuration["Serilog:MinimumLevel:Override:Microsoft.Extensions.Configuration"]);
    }

    private static void AssertTransformEx(IConfiguration configuration)
    {
        Assert.Equal("Verbose2", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime2"]);
        Assert.Equal("Warning2", configuration["Serilog:MinimumLevel:Override:Microsoft.Extensions.Configuration2"]);
        Assert.Equal("Verbose3", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime3"]);
        Assert.Equal("Verbose4", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime4"]);
        Assert.Equal("Warning5", configuration["Serilog:MinimumLevel:Override:Microsoft.Extensions.Configuration5"]);
        Assert.Equal("Verbose6", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime6"]);
        Assert.Equal("Verbose7", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime7"]);
        Assert.Equal("Warning8", configuration["Serilog:MinimumLevel:Override:Microsoft.Extensions.Configuration8"]);
    }

    [Fact]
    public void Default_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
    }

    [Fact]
    public void WithAddConfiguration_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithAddConfiguration(b => b.AddInMemoryCollection(GetInMemory()))
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "MemoryConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertInMemory(configuration);
    }

    [Fact]
    public void WithAppSecrets_Production_Success()
    {
        var builder = new ConfigurationBuilderBuilder()
            .WithAppSecrets<ConfigurationBuilderBuilderTest>();

        var configuration = builder.BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        Assert.Equal("Production", builder.EnvironmentName);
        AssertProduction(configuration);
    }

    [Fact]
    public void WithAppSecrets_Development_Success()
    {
        var builder = new ConfigurationBuilderBuilder()
            .WithEnvironment("Development")
            .WithAppSecrets<ConfigurationBuilderBuilderTest>();

        var configuration = builder.BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
            ]);

        Assert.Equal("Development", builder.EnvironmentName);
        AssertDevelopment(configuration);
    }

    [Fact]
    public void WithBaseDirectory_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithBaseDirectory()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
    }

    [Fact]
    public void WithBasePath_Empty_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithBasePath("")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
    }

    [Fact]
    public void WithBasePath_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithBasePath("New-Location")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        Assert.Equal("MainValue1-new-location", configuration["MainKey"]);
        Assert.Null(configuration["EnvironmentKey"]);
    }

    [Fact]
    public void WithCurrentDirectory_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithCurrentDirectory()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
    }

    [Fact]
    public void WithCommandLines_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithCommandLines("--Key1", "Value2a", "/Key3:Sub4", "Value5b")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        Assert.Equal("Value2a", configuration["Key1"]);
        Assert.Equal("Value5b", configuration["Key3:Sub4"]);
        AssertProduction(configuration);
    }

    [Fact]
    public void WithCommandLines_Null()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithCommandLines(null)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
        Assert.Null(configuration["CommandLineKey"]);
    }

    [Fact]
    public void WithCommandLines_Create_Null_Explicit()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(null)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
        Assert.Null(configuration["CommandLineKey"]);
    }

    [Fact]
    public void WithCommandLines_Create_Null_Implicit()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider"
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration);
        Assert.Null(configuration["CommandLineKey"]);
    }

    [Fact]
    public void WithCommandLines_NullString()
    {
        var argsHasNull = new string[] { null! };

        Assert.Throws<NullReferenceException>(() => new ConfigurationBuilderBuilder().WithCommandLines(argsHasNull).BuildConfigurationRoot());
    }

    [Fact]
    public void WithConfiguration_Success()
    {
        var initialConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(GetInMemory())
            .Build();

        var configuration = new ConfigurationBuilderBuilder()
            .WithConfiguration(initialConfiguration)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "ChainedConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                null,
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertInMemory(configuration);

        var configuration2 = new ConfigurationBuilderBuilder()
          .WithConfiguration(initialConfiguration.GetSection("Key3"))
          .BuildConfigurationRoot();

        Assert.Null(configuration2["Key1"]);
        Assert.Null(configuration2["Key3:Sub4"]);
        Assert.Equal("Value5", configuration2["Sub4"]);
    }

    [Fact]
    public void WithInMemory_Success()
    {
        var configuration = new ConfigurationBuilderBuilder()
            .WithInMemory(GetInMemory())
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "MemoryConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                null,
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertInMemory(configuration);
    }

    [Fact]
    public void Build_Production_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void Build_Production2_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
            .WithEnvironment("Production")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void Build_Production_changeTo_dev_Success()
    {
        // add file appsettings.dev.json to solve Ubuntu file name case-sensitive
        var builder = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
            .WithEnvironment("Production");

#if NET6_0_OR_GREATER
        Assert.NotNull(builder.Manager);
#else
        Assert.NotNull(builder.Builder);
#endif

        Assert.Equal("Production", builder.EnvironmentName);
        var configuration1 = builder.BuildConfigurationRoot();
        AssertProduction(configuration1);
        AssertProviders(configuration1, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        var configuration2 = builder.WithEnvironment("dev").BuildConfigurationRoot();
        Assert.Equal("dev", builder.EnvironmentName);

        AssertProviders(configuration2, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.dev.json",
                null,
                null,
            ]);

        Assert.Equal("MainValue1", configuration2["MainKey"]);
        Assert.Equal("EnvironmentDevelopmentValue1", configuration2["EnvironmentKey"]);
        Assert.Null(configuration2["DevelopmentSection:SubKey"]);
        AssertCommandLines(configuration2);
    }

    [Fact]
    public void Build_Development_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
            .WithEnvironment("Development")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
                null,
            ]);

        AssertDevelopment(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void Build_Development2_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
            .WithEnvironment("Development")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
            ]);

        AssertDevelopment(configuration);
        Assert.Null(configuration["CommandLineKey"]);
    }

    [Fact]
    public void Build_Testing_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>(GetCommandLines())
            .WithEnvironment("Testing")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Testing.json",
                null,
                null,
            ]);

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
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "XmlConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                "appsettings.ini",
                "appsettings.xml",
                null,
                null,
            ]);

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
        var configuration = builder.BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "XmlConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                "appsettings.ini",
                "appsettings.xml",
                null,
            ]);

        AssertProduction(configuration);
        Assert.Null(configuration["CommandLineKey"]);

        AssertIni(configuration);

        AssertXml(configuration);

        var configuration2 = builder.WithClearAddedConfiguration().BuildConfigurationRoot();

        AssertProviders(configuration2, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
            ]);

        AssertProduction(configuration2);
        AssertIni(configuration2, false);
        AssertXml(configuration2, false);
    }

    [Fact]
    public void Build_FullApi()
    {
        var initConfig = new ConfigurationBuilder().AddXmlFile("appsettings.xml").Build();

        string[]? args = null;
        var configuration = new ConfigurationBuilderBuilder()
            .WithBasePath("C:/App/bin")
            .WithCurrentDirectory()
            .WithBaseDirectory()
            .WithAppSecrets<ConfigurationBuilderBuilderTest>()
            .WithAppSecrets(typeof(ConfigurationBuilderBuilderTest).Assembly)
            .WithEnvironment("Staging")
            .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
            .WithCommandLines(args)
            .WithInMemory(GetInMemory())
            .WithConfiguration(initConfig)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "ChainedConfigurationProvider",
                "MemoryConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "IniConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                null,
                null,
                "appsettings.json",
                "appsettings.Staging.json",
                "appsettings.ini",
                null,
            ]);

        AssertInMemory(configuration);
        AssertIni(configuration);
        AssertXml(configuration);
    }

    // New Create API
    [Fact]
    public void CreateWithAssemblyBuild_Production_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create(typeof(ConfigurationBuilderBuilderTest).Assembly, GetCommandLines())
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void CreateWithAssemblyBuild_Production2_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create(typeof(ConfigurationBuilderBuilderTest).Assembly, GetCommandLines())
            .WithEnvironment("Production")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void CreateWithAssemblyBuild_Development_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create(typeof(ConfigurationBuilderBuilderTest).Assembly, GetCommandLines())
            .WithEnvironment("Development")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
                null,
            ]);

        AssertDevelopment(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void CreateWithoutAssemblyBuild_Production_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create(GetCommandLines())
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void CreateWithoutAssemblyBuild_Development_Success()
    {
        var configuration = ConfigurationBuilderBuilder.Create(GetCommandLines())
            .WithEnvironment("Development")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                null,
                null,
            ]);

        AssertDevelopment(configuration);
        AssertCommandLines(configuration);
    }

    [Fact]
    public void EnvironmentNameTransform()
    {
        var configuration = ConfigurationBuilderBuilder.Create()
            .WithEnvironment("Transform")
            .WithTransformConfiguration()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "TransformConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Transform.json",
                null,
                null,
            ]);

        AssertTransform(configuration);
        AssertTransformEx(configuration);
    }

    [Fact]
    public void EnvironmentTransform()
    {
        Dictionary<string, string> env = new()
        {
            { "env_Transform__0","Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Authentication=Information" },
            { "env_Transform__1__Key","Serilog:MinimumLevel" },
            { "env_Transform__1__Value","Override:Microsoft.AspNetCore.Mvc=Error" },
            { "env_Transform__2__Key","Serilog:MinimumLevel:Override" },
            { "env_Transform__2__Values__0","Microsoft.Hosting.Lifetime=Verbose" },
            { "env_Transform__2__Values__1","Microsoft.Extensions.Configuration=Warning" }
        };

        foreach (var (key, value) in env)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        var configuration = ConfigurationBuilderBuilder.Create()
            .WithTransformConfiguration("env_Transform")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "TransformConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
            ]);

        AssertTransform(configuration);
    }

    [Fact]
    public void CommandLinesTransform()
    {
        var configuration = ConfigurationBuilderBuilder.Create([
            "/tt:0=Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Authentication=Information",
            "/tt:1:Key=Serilog:MinimumLevel",
            "/tt:1:Value=Override:Microsoft.AspNetCore.Mvc=Error",
            "/tt:2:Key=Serilog:MinimumLevel:Override",
            "/tt:2:Values:0=Microsoft.Hosting.Lifetime=Verbose",
            "/tt:2:Values:1=Microsoft.Extensions.Configuration=Warning"
        ])
            .WithTransformConfiguration("tt")
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
                "TransformConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        AssertTransform(configuration);
    }

    [Fact]
    public void AddFileConfigurationDevelopmentSecrets_NoSources()
    {
        var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
            .WithEnvironment("Development")
            .WithAddFileConfiguration()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
                null,
            ]);

        AssertDevelopment(configuration);
    }

    [Fact]
    public void AddFileConfigurationProduction_configJson()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0=Add-File-Source/config.json"
            ])
            .WithAddFileConfiguration()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.json", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
    }

    [Fact]
    public void AddFileConfigurationProduction_configJsonIni()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0=Add-File-Source/config.json",
                "/AddFile:1=Add-File-Source/config.ini",
            ])
            .WithAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
            }, throwIfNotSupport: true)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.ini", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
    }

    [Fact]
    public void AddFileConfigurationProduction_configIniJson()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0=Add-File-Source/config.ini",
                "/AddFile:1=Add-File-Source/config.json",
            ])
            .WithAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
            }, throwIfNotSupport: true)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.json", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
    }

    [Fact]
    public void AddFileConfigurationProduction_configIniJsonXml()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0=Add-File-Source/config.ini",
                "/AddFile:1=Add-File-Source/config.json",
                "/AddFile:2=Add-File-Source/config.xml",
            ])
            .WithAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
                options.AddProvider(".xml", (builder, source) => builder.AddXmlFile(source.Path, source.Optional, source.ReloadOnChange));
            }, throwIfNotSupport: true)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.xml", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
        Assert.Equal("config.xml", configuration["XmlSection:Add-File-Source"]);
    }

    [Fact]
    public void AddFileConfiguration_Transform()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/configSrc:0=appsettings.Transform.json",
            ])
            .WithAddFileConfiguration(null, "configSrc")
            .WithTransformConfiguration()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
                "TransformConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertTransform(configuration);
        AssertTransformEx(configuration);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfigurationDevelopmentSecrets_NoSources()
    {
        var configuration = CreateDefaultBuilder(environmentName: "Development", type: typeof(ConfigurationBuilderBuilderTest))
            .AddAddFileConfiguration()
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Development.json",
                "secrets.json",
                null,
                null,
            ]);

        AssertDevelopment(configuration);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfigurationProduction_configJson()
    {
        var configuration = CreateDefaultBuilder([
                "/AddFile:0=Add-File-Source/config.json"
            ])
            .AddAddFileConfiguration()
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.json", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfigurationProduction_configJsonIni()
    {
        var configuration = CreateDefaultBuilder([
                "/AddFile:0=Add-File-Source/config.json",
                "/AddFile:1=Add-File-Source/config.ini",
            ])
            .AddAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
            }, throwIfNotSupport: true)
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.ini", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfigurationProduction_configIniJson()
    {
        var configuration = CreateDefaultBuilder([
                "/AddFile:0=Add-File-Source/config.ini",
                "/AddFile:1=Add-File-Source/config.json",
            ])
            .AddAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
            }, throwIfNotSupport: true)
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.json", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfigurationProduction_configIniJsonXml()
    {
#if NET6_0_OR_GREATER
#pragma warning disable 0618
#endif
        var configuration = ConfigurationBuilderBuilder.Create([
                "/AddFile:0=Add-File-Source/config.ini",
                "/AddFile:1=Add-File-Source/config.json",
                "/AddFile:2=Add-File-Source/config.xml",
            ])
            .Builder
            .AddAddFileConfiguration(options =>
            {
                options.AddProvider(".ini", IniConfigurationExtensions.AddIniFile);
                options.AddProvider(".xml", (builder, source) => builder.AddXmlFile(source.Path, source.Optional, source.ReloadOnChange));
            }, throwIfNotSupport: true)
            .Build();
#if NET6_0_OR_GREATER
#pragma warning restore 0618
#endif

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Add-File-Source/config.xml", configuration["MainKey"]);
        Assert.Equal("config.json", configuration["JsonSection:Add-File-Source"]);
        Assert.Equal("config.ini", configuration["IniSection:Add-File-Source"]);
        Assert.Equal("config.xml", configuration["XmlSection:Add-File-Source"]);
    }

    [Fact]
    public void ConfigurationBuilder_AddFileConfiguration_Transform()
    {
        var configuration = CreateDefaultBuilder([
                "/configSrc:0=appsettings.Transform.json",
            ])
            .AddAddFileConfiguration(null, "configSrc")
            .AddTransformConfiguration()
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
                "TransformConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
                null,
            ]);

        AssertProduction(configuration);
        AssertTransform(configuration);
        AssertTransformEx(configuration);
    }

    [Fact]
    public void AddFileConfigurationProduction_KeyPerFile()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0:Provider=Key-per_file",
                "/AddFile:0:Path=Key-Per-File"
            ])
            .WithAddFileConfiguration(options =>
            {
#if NET5_0_OR_GREATER
                options.AddProvider("key-per_file", KeyPerFileConfigurationBuilderExtensions.AddKeyPerFile, resolveAbsolute: true);
#else
                options.AddProvider("key-per_file", (builder, source) => builder.AddKeyPerFile(source.Path, source.Optional), resolveAbsolute: true);
#endif
            })
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Key-Per-File/mainValue line0", configuration["MainKey"]);
        Assert.Equal("KeyPerFileSection__Add-File-Source.txt", configuration["KeyPerFileSection:Add-File-Source"]);
    }

    [Fact]
    public void AddFileConfigurationProduction_KeyPerFile_Handler()
    {
        var configuration = ConfigurationBuilderBuilder.Create(
            [
                "/AddFile:0:Provider=Key-per_file",
                "/AddFile:0:Path=Key-Per-File"
            ])
            .WithAddFileConfiguration(options =>
            {
#if NET5_0_OR_GREATER
                options.AddHandler(ctx =>
                {
                    if (ctx.Configuration["Provider"] == "Key-per_file")
                    {
                        ctx.ConfigurationBuilder.AddKeyPerFile(Path.GetFullPath(ctx.Source.Path), ctx.Source.Optional, ctx.Source.ReloadOnChange);
                        ctx.IsProcessed = true;
                    }
                });
#else
                options.AddHandler(ctx =>
                {
                    if (ctx.Configuration["Provider"] == "Key-per_file")
                    {
                        ctx.ConfigurationBuilder.AddKeyPerFile(Path.GetFullPath(ctx.Source.Path), ctx.Source.Optional);
                        ctx.IsProcessed = true;
                    }
                });
#endif
            }, throwIfNotSupport: true)
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "ChainedConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Production.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("EnvironmentProductionValue1", configuration["EnvironmentKey"]);
        Assert.Equal("Key-Per-File/mainValue line0", configuration["MainKey"]);
        Assert.Equal("KeyPerFileSection__Add-File-Source.txt", configuration["KeyPerFileSection:Add-File-Source"]);
    }

    [Fact]
    public void EnvironmentNameMapConfiguration()
    {
        var configuration = ConfigurationBuilderBuilder.Create(["/CommandLineSection:SubKey=CommandLineSection-SubKeyValue"])
            .WithEnvironment("Map")
            .WithMapConfiguration()
            .BuildConfigurationRoot();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
                "MapConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Map.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("MainValue1", configuration["MainKey"]);
        Assert.Equal("MainValue1", configuration["SecondaryKey"]);
        Assert.Equal("CommandLineSection-SubKeyValue", configuration["CommandLineSection:SubKey"]);
        Assert.Equal("CommandLineSection-SubKeyValue", configuration["MapSection:SubKey"]);
    }

    [Fact]
    public void ConfigurationBuilder_EnvironmentNameMapConfiguration()
    {
        var configuration = CreateDefaultBuilder(["/CommandLineSection:SubKey=CommandLineSection-SubKeyValue"], "Map")
            .AddMapConfiguration()
            .Build();

        AssertProviders(configuration, [
                "JsonConfigurationProvider",
                "JsonConfigurationProvider",
                "EnvironmentVariablesConfigurationProvider",
                "CommandLineConfigurationProvider",
                "MapConfigurationProvider",
            ], [
                "appsettings.json",
                "appsettings.Map.json",
                null,
                null,
                null,
            ]);

        Assert.Equal("MainValue1", configuration["MainKey"]);
        Assert.Equal("MainValue1", configuration["SecondaryKey"]);
        Assert.Equal("CommandLineSection-SubKeyValue", configuration["CommandLineSection:SubKey"]);
        Assert.Equal("CommandLineSection-SubKeyValue", configuration["MapSection:SubKey"]);
    }

}
