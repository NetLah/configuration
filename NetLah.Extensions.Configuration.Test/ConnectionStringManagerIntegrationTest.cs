using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConnectionStringManagerIntegrationTest
{
    private static IConfigurationRoot NewConfiguration(IEnumerable<KeyValuePair<string, string>>? initialData = null)
    {
        var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(initialData);
        var configuration = configurationBuilder.Build();
        return configuration;
    }

    private static IConfigurationRoot GetConfiguration() => NewConfiguration(new Dictionary<string, string>
    {
        ["connectionStrings:defaultConnection"] = "defaultConnection1;",
        ["connectionStrings:defaultConnection_sqlserver"] = "server=sqlserver1;database=default;",
        ["connectionStrings:defaultConnection_mysql"] = "server=mysqL1;database=default;",
        ["connectionStrings:defaultConnection_postgresql"] = "server=postgresql1;database=default;",
        ["connectionStrings:defaultConnection_cOSmOS"] = "server=cosmos1;database=default;",
        ["connectionStrings:Defaultconnection_aNY"] = "server=any1;database=default;",
        ["connectionstrings:sqlserver2"] = "type=sqlserver;",
        ["connectionstrings:Sqlserver2_providerName"] = "sqlserver",
        ["connectionstrings:sqLServer3"] = "type=MSSQL;",
        ["connectionstrings:SQLServer3_providerName"] = "MSSQL",
        ["connectionstrings:sqLSErver4"] = "type=SQLAzure;",
        ["connectionstrings:SQLServer4_providerName"] = "SQLAzure",
        ["connectionstrings:SQLServer5"] = "type=System.Data.SqlClient;",
        ["connectionstrings:SQLServer5_providerName"] = "System.Data.SqlClient",
        ["connectionstrings:sqlSERVER6_SQLServer"] = "server=local6;",
        ["connectionstrings:sqLServer7_sqlserver"] = "server=local7;",
        ["connectionstrings:MysQL8"] = "server=local8;",
        ["connectionstrings:MysqL8_providerName"] = "mysql",
        ["connectionstrings:MySqL9"] = "server=local9;",
        ["connectionstrings:MYSql9_providerName"] = "mysql.data.mysqlclient",
        ["connectionstrings:MySqL10_mySql"] = "server=local10;",
        ["connectionstrings:postGREsql11"] = "server=local11;",
        ["connectionstrings:PostgreSql11_providerName"] = "POSTgreSQL",
        ["connectionstrings:pOSTgreSQL12_postGreSql"] = "server=local12;",
        ["connectionstrings:cosmos13"] = "server=cosmos13;",
        ["connectionstrings:cosMOS13_PROVIDERNAME"] = "cosmos",
        ["connectionstrings:COSmos14_cOSMOS"] = "server=cosmos14;",
        ["connectionstrings:Sqlserver15_mssql"] = "server=local15;",
        ["connectionstrings:Sqlserver16_sqlazure"] = "server=local16;",
        ["connectionstrings:SQLServer17"] = "type=Microsoft.Data.SqlClient;",
        ["connectionstrings:SQLServer17_providerName"] = "Microsoft.Data.SqlClient",
        ["connectionstrings:SQLServer18_Microsoft.Data.SqlClient"] = "type=Microsoft.Data.SqlClient18;",
        ["connectionstrings:MySqL19"] = "server=local19;",
        ["connectionstrings:MYSql19_providerName"] = "mysqlconnector",
        ["connectionstrings:MYSql20_MYSQLCONNECTOR"] = "server=local20;",
        ["connectionstrings:postGREsql21"] = "server=local21;",
        ["connectionstrings:PostgreSql21_providerName"] = "npgsql",
        ["connectionstrings:pOSTgreSQL22_NPGSQL"] = "server=local22;",
        ["connectionstrings:postGREsql23"] = "server=local23;",
        ["connectionstrings:PostgreSql23_providerName"] = "POSTGRES",
        ["connectionstrings:pOSTgreSQL24_postgres"] = "server=local24;",
        ["connectionstrings:Redis:configuration"] = "localhost:6379;",      // no more support complex type
        ["connectionstrings:DataProtection_Redis1:configuration"] = "localhost:36379;", // no more support complex type
    });

    private static IConnectionStringManager GetService() => GetService(GetConfiguration());
    private static IConnectionStringManager GetService(IConfiguration configuration) => new ConnectionStringManager(configuration);

    private static Entry[] GetByProviderName(string selectProviderName) => GetByProviderObject(selectProviderName);

    private static Entry[] GetByProviderObject(object selectedProvider)
        => GetArray(((ConnectionStringManager)GetService()).CloneWithProvider(selectedProvider).ConnectionStrings);

    private static Entry[] GetArray(IEnumerable<KeyValuePair<string, ProviderConnectionString>> keyValues)
        => keyValues
            .OrderBy(kv => kv.Key)
            .Select(kv => new Entry(kv.Key, kv.Value))
            .ToArray();

    [Fact]
    public void BasicExpandTest()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["a"] = "$$1 ${eee} $[Bb] 2 $( cCc ) %{ffff} ${BB} %%3 %{ DD DD }",
                ["bb"] = "BBbbBB",
                ["ccc"] = "CCCC(%(bB)-4-%[dD dd ])",
                [" dd dd  "] = "<<DD>>",
            });
        var config = configBuilder.Build();
        var manager = new ConnectionStringManager(config, "");
        var dict = manager.ConnectionStrings;

        Assert.NotNull(dict);
        var a = dict["A"];
        Assert.Equal("$1 ${eee} BBbbBB 2 CCCC(BBbbBB-4-<<DD>>) %{ffff} BBbbBB %3 <<DD>>", a.Value);
        Assert.Equal("$$1 ${eee} $[Bb] 2 $( cCc ) %{ffff} ${BB} %%3 %{ DD DD }", a.Raw);
        var b = dict["BB"];
        Assert.Equal("BBbbBB", b.Value);
        Assert.Equal("BBbbBB", b.Raw);
        var c = dict["CCC"];
        Assert.Equal("CCCC(BBbbBB-4-<<DD>>)", c.Value);
        Assert.Equal("CCCC(%(bB)-4-%[dD dd ])", c.Raw);
        var d = dict["dd dd"];
        Assert.Equal("<<DD>>", d.Value);
        Assert.Equal("<<DD>>", d.Raw);
    }

    [Fact]
    public void IndexerGetDefaultConnectionTest()
    {
        var config = NewConfiguration(new Dictionary<string, string> { ["connectionStrings:defaultConnection"] = "defaultConnection1;" });
        var connectionStringManager = GetService(config);

        var result = connectionStringManager["DefaultConnection"];

        Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        Assert.Equal("defaultConnection1;", result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        Assert.Equal(DbProviders.Custom, result.Provider);
        Assert.Null(result.Custom);
    }

    [Fact]
    public void FrameworkGetDefaultConnection()
    {
        var config = NewConfiguration(new Dictionary<string, string> { ["connectionStrings:defaultConnection"] = "defaultConnection1;" });

        Assert.Equal("defaultConnection1;", config.GetConnectionString("DefaultConnection"));
    }

    [Fact]
    public void NullConfigTest()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.ThrowsAny<ArgumentException>(() => GetService(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Theory]
    [InlineData("DefaultConnection", "defaultConnection1;", DbProviders.Custom, null)]
    [InlineData("SQLServer2", "type=sqlserver;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver3", "type=MSSQL;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver4", "type=SQLAzure;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver5", "type=System.Data.SqlClient;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver6", "server=local6;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver7", "server=local7;", DbProviders.SQLServer, null)]
    [InlineData("mysql8", "server=local8;", DbProviders.MySQL, null)]
    [InlineData("MySQL9", "server=local9;", DbProviders.MySQL, null)]
    [InlineData("MySQL10", "server=local10;", DbProviders.MySQL, null)]
    [InlineData("postgresql11", "server=local11;", DbProviders.PostgreSQL, null)]
    [InlineData("POSTGRESQL12", "server=local12;", DbProviders.PostgreSQL, null)]
    [InlineData("cosmos13", "server=cosmos13;", DbProviders.Custom, "cosmos")]
    [InlineData("COSMOS14", "server=cosmos14;", DbProviders.Custom, "cOSMOS")]
    [InlineData("Sqlserver15", "server=local15;", DbProviders.SQLServer, null)]
    [InlineData("Sqlserver16", "server=local16;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver17", "type=Microsoft.Data.SqlClient;", DbProviders.SQLServer, null)]
    [InlineData("sqlserver18", "type=Microsoft.Data.SqlClient18;", DbProviders.SQLServer, null)]
    [InlineData("mysql19", "server=local19;", DbProviders.MySQL, null)]
    [InlineData("mysql20", "server=local20;", DbProviders.MySQL, null)]
    [InlineData("postgresql21", "server=local21;", DbProviders.PostgreSQL, null)]
    [InlineData("postgresql22", "server=local22;", DbProviders.PostgreSQL, null)]
    [InlineData("postgresql23", "server=local23;", DbProviders.PostgreSQL, null)]
    [InlineData("postgresql24", "server=local24;", DbProviders.PostgreSQL, null)]
    [InlineData("NoExist", null, DbProviders.Custom, null)]
    public void GetConnectionTest(string connectionName, string expectedConnectionString, DbProviders expectedProvider, string expectedCustom)
    {
        var result = GetService()[connectionName];

        if (expectedConnectionString == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expectedConnectionString, result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expectedProvider, result.Provider);
            Assert.Equal(expectedCustom, result.Custom);
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("defaultConnection1;", "   default  Connection   ")]
    [InlineData("empty3;", "")]
    [InlineData("empty3;", "   ")]
    public void GetNullEmptyOrSpaceConnectionNameKeyTrimTest(string expected, string connectionName)
    {
        var config = NewConfiguration(new Dictionary<string, string>
        {
            ["emptyOrSpace: default  Connection "] = "defaultConnection1;",
            ["emptyOrSpace:"] = "empty3;",
            ["emptyOrSpace: "] = "oneSpace4;"
        });
        var connectionStringManager = new ConnectionStringManager(config, "emptyOrSpace");

        var result = connectionStringManager[connectionName];

        if (expected == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Raw);
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("defaultConnection1;", "default  Connection")]
    [InlineData("empty3;", "")]
    [InlineData("oneSpace4;", " ")]
    [InlineData("twoSpace5;", "  ")]
    [InlineData(null, "   ")]
    public void GetNullEmptyOrSpaceConnectionName_WithKeyPreserveSpace_Test(string expected, string connectionName)
    {
        var config = NewConfiguration(new Dictionary<string, string>
        {
            ["emptyOrSpace:default  Connection"] = "defaultConnection1;",
            ["emptyOrSpace:"] = "empty3;",
            ["emptyOrSpace: "] = "oneSpace4;",
            ["emptyOrSpace:  "] = "twoSpace5;",
        });
        var original = new ConnectionStringManager(config, "emptyOrSpace");
        var connectionStringManager = original.CloneWithKeyPreserveSpace();

        Assert.NotSame(original.Root, ((ConnectionStringManager)connectionStringManager).Root);
        Assert.Same(((ConnectionStringManager)connectionStringManager).Root, ((ConnectionStringManager)connectionStringManager.CloneWithKeyPreserveSpace()).Root);

        var result = connectionStringManager[connectionName];

        if (expected == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Raw);
        }
    }

    [Theory]
    [InlineData("connectionStrings")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("any:other")]
    public void SectionNameTest(string sectionName)
    {
        var sectionNameColon = string.IsNullOrWhiteSpace(sectionName) ? sectionName : sectionName + ":";
        var config = NewConfiguration(new Dictionary<string, string> { [$"{sectionNameColon}defaultConnection"] = "defaultConnection1;" });
        var connectionStringManager = new ConnectionStringManager(config, sectionName);

        var result = connectionStringManager["defaultConnection"];

        Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        Assert.Equal("defaultConnection1;", result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        Assert.Equal("defaultConnection1;", result.Raw);
    }

    [Theory]
    [InlineData(null, "not-exist")]
    [InlineData("oneSpace3;", "")]
    [InlineData(null, null)]
    [InlineData("defaultConnection1;", "defaultConnection")]
    [InlineData("defaultConnection1;", null, "not-exist", "defaultConnection")]
    [InlineData("cosmos2;", null, "not-exist", "cosmos", "defaultConnection")]
    [InlineData("defaultConnection1;", null, "not-exist", "defaultConnection", "cosmos")]
    [InlineData("oneSpace3;", null, "not-exist", " ", "defaultConnection")]
    public void MultiConnectionNamesTest_WithKeyTrim(string expected, string connectionName, params string[] connectionNames)
    {
        var config = NewConfiguration(new Dictionary<string, string>
        {
            ["section:defaultConnection"] = "defaultConnection1;",
            ["section:cosmos"] = "cosmos2;",
            ["section: "] = "oneSpace3;",
        });
        var connectionStringManager = new ConnectionStringManager(config, "section");

        var result = connectionStringManager[connectionName, connectionNames];

        if (expected == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Raw);
        }
    }

    [Theory]
    [InlineData(null, "not-exist")]
    [InlineData(null, "")]
    [InlineData(null, null)]
    [InlineData("defaultConnection1;", "defaultConnection")]
    [InlineData("defaultConnection1;", null, "", null, "not-exist", "defaultConnection")]
    [InlineData("cosmos2;", null, "", null, "not-exist", "cosmos", "defaultConnection")]
    [InlineData("defaultConnection1;", null, "", null, "not-exist", "defaultConnection", "cosmos")]
    [InlineData("oneSpace3;", null, "", null, "not-exist", " ", "defaultConnection", "cosmos")]
    public void MultiConnectionNamesTest_WithKeyPreserveSpace(string expected, string connectionName, params string[] connectionNames)
    {
        var config = NewConfiguration(new Dictionary<string, string>
        {
            ["section:defaultConnection"] = "defaultConnection1;",
            ["section:cosmos"] = "cosmos2;",
            ["section: "] = "oneSpace3;",
        });

        var original = new ConnectionStringManager(config, "section");
        var connectionStringManager = original.CloneWithKeyPreserveSpace();

        Assert.NotSame(original.Root, ((ConnectionStringManager)connectionStringManager).Root);
        Assert.Same(((ConnectionStringManager)connectionStringManager).Root, ((ConnectionStringManager)connectionStringManager.CloneWithKeyPreserveSpace()).Root);

        var result = connectionStringManager[connectionName, connectionNames];

        if (expected == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.Equal(expected, result.Raw);
        }
    }

    [Fact]
    public void WithProviderTest()
    {
        var config = NewConfiguration(new Dictionary<string, string>
        {
            ["default"] = "defaultConnection1;",
            ["dEfault_sqlserver"] = "defaultSqlServer2;",
            ["deFault_postgresql"] = "defaultPostgreSQL3;",
            ["defAult_cosmos"] = "defaultCosmos5;",
        });

        var service = new ConnectionStringManager(config, "");
        Assert.NotEmpty(service.ConnectionStrings);
        var connStr1 = service["Default"];
        Assert.NotNull(connStr1);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        Assert.Equal("default", connStr1.Name);
        Assert.Equal("defaultConnection1;", connStr1.Value);
        Assert.NotEmpty(service.ConnectionStrings);

        var service2 = service.CloneWithProvider(DbProviders.SQLServer);
        var connStr2 = service2["Default"];
        Assert.NotNull(connStr2);
        Assert.Equal("dEfault", connStr2.Name);
        Assert.Equal("defaultSqlServer2;", connStr2.Value);
        Assert.NotEmpty(service2.ConnectionStrings);

        var service3 = service.CloneWithProvider(DbProviders.PostgreSQL);
        var connStr3 = service3["Default"];
        Assert.NotNull(connStr3);
        Assert.Equal("deFault", connStr3.Name);
        Assert.Equal("defaultPostgreSQL3;", connStr3.Value);
        Assert.NotEmpty(service3.ConnectionStrings);

        var service4 = service.CloneWithProvider(DbProviders.MySQL);
        var connStr4 = service4["Default"];
        Assert.Null(connStr4);
        Assert.Empty(service4.ConnectionStrings);

        var service5 = service.CloneWithProvider("Cosmos");
        var connStr5 = service5["Default"];
        Assert.NotNull(connStr5);
        Assert.Equal("defAult", connStr5.Name);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        Assert.Equal("defaultCosmos5;", connStr5.Value);
        Assert.NotEmpty(service5.ConnectionStrings);
    }

    [Fact]
    public void AllConnectionStringsTest()
    {
        AssertAll(GetArray(GetService().ConnectionStrings));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AllConnectionStringsSelectNullOrEmptyTest(string selectProviderName)
    {
        AssertAll(GetByProviderName(selectProviderName));
    }

    private static void AssertAll(Entry[] allConnectionStrings)
    {
        Assert.Equal(new Entry[] {
                new Entry("cosmos13", new ProviderConnectionString ("cosmos13", "server=cosmos13;", DbProviders.Custom, "cosmos")),
                new Entry("COSmos14", new ProviderConnectionString ("COSmos14", "server=cosmos14;", DbProviders.Custom, "cOSMOS")),
                new Entry("COSmos14_cOSMOS", new ProviderConnectionString ("COSmos14_cOSMOS", "server=cosmos14;", DbProviders.Custom)),
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "defaultConnection1;")),
                new Entry("Defaultconnection_aNY", new ProviderConnectionString ("Defaultconnection_aNY", "server=any1;database=default;")),
                new Entry("defaultConnection_cOSmOS", new ProviderConnectionString ("defaultConnection_cOSmOS", "server=cosmos1;database=default;")),
                new Entry("MySqL10", new ProviderConnectionString("MySqL10", "server=local10;", DbProviders.MySQL)),
                new Entry("MySqL19", new ProviderConnectionString("MySqL19", "server=local19;", DbProviders.MySQL)),
                new Entry("MYSql20", new ProviderConnectionString("MYSql20", "server=local20;", DbProviders.MySQL)),
                new Entry("MysQL8", new ProviderConnectionString("MysQL8", "server=local8;", DbProviders.MySQL)),
                new Entry("MySqL9", new ProviderConnectionString("MySqL9", "server=local9;", DbProviders.MySQL)),
                new Entry("postGREsql11", new ProviderConnectionString("postGREsql11", "server=local11;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL12", new ProviderConnectionString("pOSTgreSQL12", "server=local12;", DbProviders.PostgreSQL)),
                new Entry("postGREsql21", new ProviderConnectionString("postGREsql21", "server=local21;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL22", new ProviderConnectionString("pOSTgreSQL22", "server=local22;", DbProviders.PostgreSQL)),
                new Entry("postGREsql23", new ProviderConnectionString("postGREsql23", "server=local23;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL24", new ProviderConnectionString("pOSTgreSQL24", "server=local24;", DbProviders.PostgreSQL)),
                new Entry("Sqlserver15", new ProviderConnectionString("Sqlserver15", "server=local15;", DbProviders.SQLServer)),
                new Entry("Sqlserver16", new ProviderConnectionString("Sqlserver16", "server=local16;", DbProviders.SQLServer)),
                new Entry("SQLServer17", new ProviderConnectionString("SQLServer17", "type=Microsoft.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("SQLServer18", new ProviderConnectionString("SQLServer18", "type=Microsoft.Data.SqlClient18;", DbProviders.SQLServer)),
                new Entry("sqlserver2", new ProviderConnectionString("sqlserver2", "type=sqlserver;", DbProviders.SQLServer)),
                new Entry("sqLServer3", new ProviderConnectionString("sqLServer3", "type=MSSQL;", DbProviders.SQLServer)),
                new Entry("sqLSErver4", new ProviderConnectionString("sqLSErver4", "type=SQLAzure;", DbProviders.SQLServer)),
                new Entry("SQLServer5", new ProviderConnectionString("SQLServer5", "type=System.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("sqlSERVER6", new ProviderConnectionString("sqlSERVER6", "server=local6;", DbProviders.SQLServer)),
                new Entry("sqLServer7", new ProviderConnectionString("sqLServer7", "server=local7;", DbProviders.SQLServer)),
            },
        allConnectionStrings,
        EntryComparer.Instance);
    }

    [Theory]
    [InlineData("Cosmos")]
    [InlineData("COSMOS")]
    public void SelectCustomNameCosmosTest(string selectProviderName)
    {
        var connectionStrings = GetByProviderName(selectProviderName);

        Assert.Equal(new Entry[] {
                new Entry("cosmos13", new ProviderConnectionString ("cosmos13", "server=cosmos13;", DbProviders.Custom, selectProviderName)),
                new Entry("COSmos14", new ProviderConnectionString ("COSmos14", "server=cosmos14;", DbProviders.Custom, selectProviderName)),
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "server=cosmos1;database=default;", DbProviders.Custom,selectProviderName)),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Theory]
    [InlineData("any")]
    [InlineData("ANY")]
    public void SelectCustomNameAnyTest(string selectProviderName)
    {
        var connectionStrings = GetByProviderName(selectProviderName);

        Assert.Equal(new Entry[] {
                new Entry("Defaultconnection", new ProviderConnectionString("Defaultconnection", "server=any1;database=default;", DbProviders.Custom, selectProviderName)),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Fact]
    public void SelectCustomNameNotExistProviderTest()
    {
        var connectionStrings = GetByProviderName("NotExistProvider");

        Assert.NotNull(connectionStrings);
        Assert.Empty(connectionStrings);
    }

    [Theory]
    [InlineData(DbProviders.Custom)]
    [InlineData("Custom")]
    public void SelectCustomTest(object selectedProvider)
    {
        var connectionStrings = GetByProviderObject(selectedProvider);

        Assert.Equal(new Entry[] {
                new Entry("cosmos13", new ProviderConnectionString ("cosmos13", "server=cosmos13;", DbProviders.Custom, "cosmos")),
                new Entry("COSmos14", new ProviderConnectionString ("COSmos14", "server=cosmos14;", DbProviders.Custom, "cOSMOS")),
                new Entry("COSmos14_cOSMOS", new ProviderConnectionString ("COSmos14_cOSMOS", "server=cosmos14;", DbProviders.Custom)),
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "defaultConnection1;")),
                new Entry("Defaultconnection_aNY", new ProviderConnectionString ("Defaultconnection_aNY", "server=any1;database=default;")),
                new Entry("defaultConnection_cOSmOS", new ProviderConnectionString ("defaultConnection_cOSmOS","server=cosmos1;database=default;")),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Theory]
    [InlineData(DbProviders.MySQL)]
    [InlineData("MySQL")]
    public void SelectMySQLTest(object selectedProvider)
    {
        var connectionStrings = GetByProviderObject(selectedProvider);

        Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "server=mysqL1;database=default;", DbProviders.MySQL)),
                new Entry("MySqL10", new ProviderConnectionString("MySqL10", "server=local10;", DbProviders.MySQL)),
                new Entry("MySqL19", new ProviderConnectionString("MySqL19", "server=local19;", DbProviders.MySQL)),
                new Entry("MYSql20", new ProviderConnectionString("MYSql20", "server=local20;", DbProviders.MySQL)),
                new Entry("MysQL8", new ProviderConnectionString("MysQL8", "server=local8;", DbProviders.MySQL)),
                new Entry("MySqL9", new ProviderConnectionString("MySqL9", "server=local9;", DbProviders.MySQL)),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Theory]
    [InlineData(DbProviders.PostgreSQL)]
    [InlineData("PostgreSQL")]
    public void SelectPostgreSQLTest(object selectedProvider)
    {
        var connectionStrings = GetByProviderObject(selectedProvider);

        Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "server=postgresql1;database=default;", DbProviders.PostgreSQL)),
                new Entry("postGREsql11", new ProviderConnectionString("postGREsql11", "server=local11;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL12", new ProviderConnectionString("pOSTgreSQL12", "server=local12;", DbProviders.PostgreSQL)),
                new Entry("postGREsql21", new ProviderConnectionString("postGREsql21", "server=local21;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL22", new ProviderConnectionString("pOSTgreSQL22", "server=local22;", DbProviders.PostgreSQL)),
                new Entry("postGREsql23", new ProviderConnectionString("postGREsql23", "server=local23;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL24", new ProviderConnectionString("pOSTgreSQL24", "server=local24;", DbProviders.PostgreSQL)),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Theory]
    [InlineData(DbProviders.SQLServer)]
    [InlineData("SQLSERVER")]
    public void SelectSqlServerTest(object selectedProvider)
    {
        var connectionStrings = GetByProviderObject(selectedProvider);

        Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ProviderConnectionString ("defaultConnection", "server=sqlserver1;database=default;", DbProviders.SQLServer)),
                new Entry("Sqlserver15", new ProviderConnectionString("Sqlserver15", "server=local15;", DbProviders.SQLServer)),
                new Entry("Sqlserver16", new ProviderConnectionString("Sqlserver16", "server=local16;", DbProviders.SQLServer)),
                new Entry("SQLServer17", new ProviderConnectionString("SQLServer17", "type=Microsoft.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("SQLServer18", new ProviderConnectionString("SQLServer18", "type=Microsoft.Data.SqlClient18;", DbProviders.SQLServer)),
                new Entry("sqlserver2", new ProviderConnectionString("sqlserver2", "type=sqlserver;", DbProviders.SQLServer)),
                new Entry("sqLServer3", new ProviderConnectionString("sqLServer3", "type=MSSQL;", DbProviders.SQLServer)),
                new Entry("sqLSErver4", new ProviderConnectionString("sqLSErver4", "type=SQLAzure;", DbProviders.SQLServer)),
                new Entry("SQLServer5", new ProviderConnectionString("SQLServer5", "type=System.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("sqlSERVER6", new ProviderConnectionString("sqlSERVER6", "server=local6;", DbProviders.SQLServer)),
                new Entry("sqLServer7", new ProviderConnectionString("sqLServer7", "server=local7;", DbProviders.SQLServer)),
            },
        connectionStrings,
        EntryComparer.Instance);
    }

    [Fact]
    public void InvalidSelectObjectType()
    {
        var connectionStringManager = GetService();

        var result = Assert.ThrowsAny<InvalidOperationException>(() => connectionStringManager.CloneWithProvider(1));

        Assert.Equal("'selectingProvider' only supported type DbProviders or System.String (provided type 'System.Int32')", result.Message);
    }

    [Fact]
    public void TestUpdateConfiguration()
    {
        var config = NewConfiguration();

        Assert.Null(config["newKey"]);

        config["newKey"] = "newValue";
        Assert.Equal("newValue", config["newKey"]);

        config["newKey"] = null;
        Assert.Null(config["newKey"]);

        config["newKey"] = "newValue2";
        Assert.Equal("newValue2", config["newKey"]);
    }

    [Theory]
    [InlineData("name1", "name2", "value3", DbProviders.SQLServer, "any", "name1", "name2", "value3", DbProviders.SQLServer, null, true)]
    [InlineData("name1", "name2", "value3", DbProviders.SQLServer, null, "name1", "name2", "value3a", DbProviders.SQLServer, null, false)]
    [InlineData("name1", "name2", "value3", DbProviders.MySQL, "", "name1", "name2", "value3", DbProviders.MySQL, "any", true)]
    [InlineData("name1", "name2", "value3", DbProviders.MySQL, "custom1", "name1", "name2b", "value3", DbProviders.MySQL, "custom1", false)]
    [InlineData("name1", "name2", "value3", DbProviders.PostgreSQL, "custom1", "name1", "name2", "value3", DbProviders.PostgreSQL, null, true)]
    [InlineData("name1", "name2", "value3", DbProviders.PostgreSQL, "", "name1c", "name2", "value3", DbProviders.PostgreSQL, null, false)]
    [InlineData("name1", "name2", "value3", DbProviders.Custom, "custom1", "name1", "name2", "value3", DbProviders.Custom, "custom1", true)]
    [InlineData("name1", "name2", "value3", DbProviders.Custom, "custom1", "name1", "name2", "value3", DbProviders.Custom, "custom2", false)]
    [InlineData("name1", "name2", "value3", DbProviders.SQLServer, null, "name1", "name2", "value3", DbProviders.MySQL, null, false)]
    [InlineData("name1", "name2", "value3", DbProviders.SQLServer, null, "name1", "name2", "value3", DbProviders.PostgreSQL, null, false)]
    [InlineData("name1", "name2", "value3", DbProviders.MySQL, null, "name1", "name2", "value3", DbProviders.PostgreSQL, null, false)]
    public void EntryComparer_Failed(
        string name1, string name1a, string value1, DbProviders provider1, string custom1,
        string name2, string name2a, string value2, DbProviders provider2, string custom2, bool expected)
    {
        var a = new Entry(name1, new ProviderConnectionString(name1a, value1, provider1, custom1));
        var b = new Entry(name2, new ProviderConnectionString(name2a, value2, provider2, custom2));
        IEqualityComparer<Entry> comparer = EntryComparer.Instance;

        var ha = comparer.GetHashCode(a);
        var hb = comparer.GetHashCode(b);

        if (expected)
        {
            Assert.True(comparer.Equals(a, b));
            Assert.NotEqual(0, ha);
            Assert.Equal(ha, hb);
        }
        else
        {
            Assert.False(comparer.Equals(a, b));
            Assert.NotEqual(0, ha);
            Assert.NotEqual(0, hb);
            Assert.NotEqual(ha, hb);
        }
    }
}
