# NetLah.Extensions.Configuration - .NET Library

[NetLah.Extensions.Configuration](https://www.nuget.org/packages/NetLah.Extensions.Configuration/) is a library which contains a set of reusable classes for build configuration with environment. These library classes are `ConfigurationBuilderBuilder` and `CertificateLoader`.

## Getting started

ConsoleApp

```
public static void Main(string[] args)
{
    var configuration = ConfigurationBuilderBuilder.Create<Program>(args).Build();
    var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"[TRACE] ConnectionString: {defaultConnectionString}");
}
```

Full API support

```
IConfigurationRoot configuration = new ConfigurationBuilderBuilder()
    .WithBasePath("C:/App/bin")
    .WithCurrentDirectory()
    .WithBaseDirectory()
    .WithAppSecrets<Program>()
    .WithAppSecrets(typeof(Program).Assembly)
    .WithEnvironment("Staging")
    .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
    .WithCommandLines(args)
    .Build();
```

## Order of Precedence when Configuring

- Reference

https://devblogs.microsoft.com/premier-developer/order-of-precedence-when-configuring-asp-net-core/

https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#default-configuration

- Order of Precedence

1. Host configuration from environment variables by prefix `DOTNET_` and `ASPNETCORE_`
2. appsettings.json using the JSON configuration provider
3. appsettings.EnvironmentName.json using the JSON configuration provider
4. Other extra configuration sources
5. App secrets when the app runs in the `Development` environment
6. Environment variables using the Environment Variables configuration provider
7. Command-line arguments using the Command-line configuration provider

## BasePath of configuration files

The application binary folder is default basePath for appsettings.json, appsettings.Production.json,etc. In case want to change current directory as basePath:

```
var configuration = new ConfigurationBuilderBuilder()
    .WithCurrentDirectory()
    .Build();
```

## Environment name

### `Production` environmentName by default if no host environmentName configuration

`ConfigurationBuilderBuilder` will detect `EnvironmentName` by add configuration environment variables with prefix `DOTNET_` and `ASPNETCORE_`. If no environment variable set, `Production` will use by default. Example of environment variables:

```
ASPNETCORE_ENVIRONMENT = Development
DOTNET_ENVIRONMENT = Staging
```

### Specifying environmentName during build configuration

Sometime, we cannot set the environmentName using environment variable, or we need different environment configuration build lik in unit test project, we can specific the environmentName.

```
var configuration = ConfigurationBuilderBuilder.Create<ConfigurationBuilderBuilderTest>()
    .WithEnvironment("Testing")
    .Build();
```

## Add extra configuration sources

```
var configuration = ConfigurationBuilderBuilder.Create<Program>()
    .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
    .WithAddConfiguration(cb => cb.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true))
    .Build();
```

Or

```
var configuration = ConfigurationBuilderBuilder.Create<Program>()
    .WithAddConfiguration(
        cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true)
            .AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
    )
    .Build();
```
