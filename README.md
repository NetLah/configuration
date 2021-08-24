# NetLah.Extensions.Configuration - .NET Library

[NetLah.Extensions.Configuration](https://www.nuget.org/packages/NetLah.Extensions.Configuration/) is a library which contains a set of reusable classes for build configuration with environment. These library classes are `ConfigurationBuilderBuilder`, `CertificateLoader` and `ConnectionStringsHelper`.

## Nuget package

[![NuGet](https://img.shields.io/nuget/v/NetLah.Extensions.Configuration.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/NetLah.Extensions.Configuration/)

## Build Status

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2FNetLah%2Fconfiguration%2Fbadge%3Fref%3Dmain&style=flat)](https://actions-badge.atrox.dev/NetLah/configuration/goto?ref=main)

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
var initConfig = new ConfigurationBuilder().Build();
IConfigurationRoot configuration = new ConfigurationBuilderBuilder()
    .WithConfiguration(initConfig)
    .WithInMemory(new Dictionary<string, string>{ ["Key:Sub"] = "Value" })
    .WithBasePath("C:/App/bin")
    .WithCurrentDirectory()
    .WithBaseDirectory()
    .WithAppSecrets<Program>()
    .WithAppSecrets(typeof(Program).Assembly)
    .WithAddConfiguration(cb => cb.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true))
    .WithEnvironment("Staging")
    .WithCommandLines(args)
    .Build();
```

## Order of Precedence when Configuring

- Reference

https://devblogs.microsoft.com/premier-developer/order-of-precedence-when-configuring-asp-net-core/

https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#default-configuration

- Order of Precedence

1. Host configuration from environment variables by prefix `DOTNET_` and `ASPNETCORE_`
2. Chanined configuration (if any)
3. In memory configuration (if any)
4. appsettings.json using the JSON configuration provider
5. appsettings.EnvironmentName.json using the JSON configuration provider
6. Other extra configuration sources
7. App secrets when the app runs in the `Development` environment
8. Environment variables using the Environment Variables configuration provider
9. Command-line arguments using the Command-line configuration provider

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

## Helper parse connectionStrings from Configuratin with database provider information

Reference at https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?#connection-string-prefixes

### Support provider

```
public enum DbProviders
{
    Custom,
    SQLServer,
    PostgreSQL,
    MySQL,
}
```

List of supported provider name

```
SQLServer
Mssql
SQLAzure
System.Data.SqlClient
Microsoft.Data.SqlClient

MySQL
MySql.Data.MySqlClient
MySqlConnector

PostgreSQL
Npgsql
Postgres
```

### Configuration appsettings.json or environment

```
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=dbname;Integrated Security=True;",
    "DefaultConnection_ProviderName": "System.Data.SqlClient",
    "BlogConnection": "AccountEndpoint=https://7d48.documents.azure.com:443/;",
    "BlogConnection_ProviderName": "Cosmos1"
}

```

### Usage:

```
IConfiguration configuration;
var connStrsHelper = new ConnectionStringsHelper(configuration);
var conn = connStrsHelper["defaultConnection"];
if (conn != null) {
    if (conn.Provider == DbProviders.PostgreSQL) {
        ...
    } else if (conn.Provider == DbProviders.MySQL) {
        ...
    } else if (conn.Provider == DbProviders.SQLServer) {
        ...
    } else if (conn.Provider == DbProviders.Custom && conn.Custom == "Cosmos1") {
        ...
    }
}
```

### Troubleshooting appsettings, configuration and connection strings

Use docker for troubleshooting

https://github.com/NetLah/EchoServiceApi

https://hub.docker.com/r/netlah/echo-service-api
