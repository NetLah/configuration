using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NetLah.Extensions.Configuration.Test
{
    public class ConnectionStringsHelperTest
    {
        private static IConfigurationRoot NewConfiguration(IEnumerable<KeyValuePair<string, string>> initialData = null)
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
        });

        private static ConnectionStringsHelper GetService() => new(GetConfiguration());

        private static Entry[] GetByProviderName(string selectProviderName) => GetByProviderObject(selectProviderName);

        private static Entry[] GetByProviderObject(object selectedProvider)
            => GetService()
                .ParseConnectionStrings(selectedProvider)
                .OrderBy(kv => kv.Key)
                .Select(kv => new Entry(kv.Key, kv.Value))
                .ToArray();

        [Fact]
        public void IndexerGetDefaultConnectionTest()
        {
            var config = NewConfiguration(new Dictionary<string, string> { ["connectionStrings:defaultConnection"] = "defaultConnection1;" });
            var connectionStringsHelper = new ConnectionStringsHelper(config);

            ConnectionStringInfo result = connectionStringsHelper["DefaultConnection"];

            Assert.NotNull(result);
            Assert.Equal("defaultConnection1;", result.ConnectionString);
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
            Assert.ThrowsAny<ArgumentException>(() => new ConnectionStringsHelper(null));
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
        public void IndexerGetConnectionTest(string connectionName, string expectedConnectionString, DbProviders expectedProvider, string expectedCustom)
        {
            ConnectionStringInfo result = GetService()[connectionName];

            if (expectedConnectionString == null)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Equal(expectedConnectionString, result.ConnectionString);
                Assert.Equal(expectedProvider, result.Provider);
                Assert.Equal(expectedCustom, result.Custom);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("      ")]
        public void IndexerGetNullConnectionNameTest(string connectionName)
        {
            var connectionStringsHelper = GetService();

            var result = Assert.ThrowsAny<ArgumentException>(() => connectionStringsHelper[connectionName]);

            Assert.Equal("connectionName is required", result.Message);
        }

        [Fact]
        public void AllConnectionStringsTest()
        {
            var allConnectionStrings = GetService().ConnectionStrings
                .OrderBy(kv => kv.Key)
                .Select(kv => new Entry(kv.Key, kv.Value))
                .ToArray();

            Assert.Equal(new Entry[] {
                new Entry("cosmos13", new ConnectionStringInfo ("server=cosmos13;", DbProviders.Custom, "cosmos")),
                new Entry("COSmos14", new ConnectionStringInfo ("server=cosmos14;", DbProviders.Custom, "cOSMOS")),
                new Entry("defaultConnection", new ConnectionStringInfo ("defaultConnection1;")),
                new Entry("MySqL10", new ConnectionStringInfo("server=local10;", DbProviders.MySQL)),
                new Entry("MySqL19", new ConnectionStringInfo("server=local19;", DbProviders.MySQL)),
                new Entry("MYSql20", new ConnectionStringInfo("server=local20;", DbProviders.MySQL)),
                new Entry("MysQL8", new ConnectionStringInfo("server=local8;", DbProviders.MySQL)),
                new Entry("MySqL9", new ConnectionStringInfo("server=local9;", DbProviders.MySQL)),
                new Entry("postGREsql11", new ConnectionStringInfo("server=local11;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL12", new ConnectionStringInfo("server=local12;", DbProviders.PostgreSQL)),
                new Entry("postGREsql21", new ConnectionStringInfo("server=local21;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL22", new ConnectionStringInfo("server=local22;", DbProviders.PostgreSQL)),
                new Entry("postGREsql23", new ConnectionStringInfo("server=local23;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL24", new ConnectionStringInfo("server=local24;", DbProviders.PostgreSQL)),
                new Entry("Sqlserver15", new ConnectionStringInfo("server=local15;", DbProviders.SQLServer)),
                new Entry("Sqlserver16", new ConnectionStringInfo("server=local16;", DbProviders.SQLServer)),
                new Entry("SQLServer17", new ConnectionStringInfo("type=Microsoft.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("SQLServer18", new ConnectionStringInfo("type=Microsoft.Data.SqlClient18;", DbProviders.SQLServer)),
                new Entry("sqlserver2", new ConnectionStringInfo("type=sqlserver;", DbProviders.SQLServer)),
                new Entry("sqLServer3", new ConnectionStringInfo("type=MSSQL;", DbProviders.SQLServer)),
                new Entry("sqLSErver4", new ConnectionStringInfo("type=SQLAzure;", DbProviders.SQLServer)),
                new Entry("SQLServer5", new ConnectionStringInfo("type=System.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("sqlSERVER6", new ConnectionStringInfo("server=local6;", DbProviders.SQLServer)),
                new Entry("sqLServer7", new ConnectionStringInfo("server=local7;", DbProviders.SQLServer)),
            },
            allConnectionStrings,
            new EntryComparer());
        }

        [Theory]
        [InlineData("Cosmos")]
        [InlineData("COSMOS")]
        public void SelectCustomNameCosmosTest(string selectProviderName)
        {
            var connectionStrings = GetByProviderName(selectProviderName);

            Assert.Equal(new Entry[] {
                new Entry("cosmos13", new ConnectionStringInfo ("server=cosmos13;", DbProviders.Custom,selectProviderName)),
                new Entry("COSmos14", new ConnectionStringInfo ("server=cosmos14;", DbProviders.Custom, selectProviderName)),
                new Entry("defaultConnection", new ConnectionStringInfo ("server=cosmos1;database=default;", DbProviders.Custom,selectProviderName)),
            },
            connectionStrings,
            new EntryComparer());
        }

        [Theory]
        [InlineData("any")]
        [InlineData("ANY")]
        public void SelectCustomNameAnyTest(string selectProviderName)
        {
            var connectionStrings = GetByProviderName(selectProviderName);

            Assert.Equal(new Entry[] {
                new Entry("Defaultconnection", new ConnectionStringInfo("server=any1;database=default;", DbProviders.Custom, selectProviderName)),
            },
            connectionStrings,
            new EntryComparer());
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
                new Entry("cosmos13", new ConnectionStringInfo ("server=cosmos13;", DbProviders.Custom, "cosmos")),
                new Entry("COSmos14", new ConnectionStringInfo ("server=cosmos14;", DbProviders.Custom, "cOSMOS")),
                new Entry("defaultConnection", new ConnectionStringInfo ("defaultConnection1;")),
            },
            connectionStrings,
            new EntryComparer());
        }

        [Theory]
        [InlineData(DbProviders.MySQL)]
        [InlineData("MySQL")]
        public void SelectMySQLTest(object selectedProvider)
        {
            var connectionStrings = GetByProviderObject(selectedProvider);

            Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ConnectionStringInfo ("server=mysqL1;database=default;", DbProviders.MySQL)),
                new Entry("MySqL10", new ConnectionStringInfo("server=local10;", DbProviders.MySQL)),
                new Entry("MySqL19", new ConnectionStringInfo("server=local19;", DbProviders.MySQL)),
                new Entry("MYSql20", new ConnectionStringInfo("server=local20;", DbProviders.MySQL)),
                new Entry("MysQL8", new ConnectionStringInfo("server=local8;", DbProviders.MySQL)),
                new Entry("MySqL9", new ConnectionStringInfo("server=local9;", DbProviders.MySQL)),
            },
            connectionStrings,
            new EntryComparer());
        }

        [Theory]
        [InlineData(DbProviders.PostgreSQL)]
        [InlineData("PostgreSQL")]
        public void SelectPostgreSQLTest(object selectedProvider)
        {
            var connectionStrings = GetByProviderObject(selectedProvider);

            Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ConnectionStringInfo ("server=postgresql1;database=default;", DbProviders.PostgreSQL)),
                new Entry("postGREsql11", new ConnectionStringInfo("server=local11;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL12", new ConnectionStringInfo("server=local12;", DbProviders.PostgreSQL)),
                new Entry("postGREsql21", new ConnectionStringInfo("server=local21;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL22", new ConnectionStringInfo("server=local22;", DbProviders.PostgreSQL)),
                new Entry("postGREsql23", new ConnectionStringInfo("server=local23;", DbProviders.PostgreSQL)),
                new Entry("pOSTgreSQL24", new ConnectionStringInfo("server=local24;", DbProviders.PostgreSQL)),
            },
            connectionStrings,
            new EntryComparer());
        }

        [Theory]
        [InlineData(DbProviders.SQLServer)]
        [InlineData("SQLSERVER")]
        public void SelectSqlServerTest(object selectedProvider)
        {
            var connectionStrings = GetByProviderObject(selectedProvider);

            Assert.Equal(new Entry[] {
                new Entry("defaultConnection", new ConnectionStringInfo ("server=sqlserver1;database=default;", DbProviders.SQLServer)),
                new Entry("Sqlserver15", new ConnectionStringInfo("server=local15;", DbProviders.SQLServer)),
                new Entry("Sqlserver16", new ConnectionStringInfo("server=local16;", DbProviders.SQLServer)),
                new Entry("SQLServer17", new ConnectionStringInfo("type=Microsoft.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("SQLServer18", new ConnectionStringInfo("type=Microsoft.Data.SqlClient18;", DbProviders.SQLServer)),
                new Entry("sqlserver2", new ConnectionStringInfo("type=sqlserver;", DbProviders.SQLServer)),
                new Entry("sqLServer3", new ConnectionStringInfo("type=MSSQL;", DbProviders.SQLServer)),
                new Entry("sqLSErver4", new ConnectionStringInfo("type=SQLAzure;", DbProviders.SQLServer)),
                new Entry("SQLServer5", new ConnectionStringInfo("type=System.Data.SqlClient;", DbProviders.SQLServer)),
                new Entry("sqlSERVER6", new ConnectionStringInfo("server=local6;", DbProviders.SQLServer)),
                new Entry("sqLServer7", new ConnectionStringInfo("server=local7;", DbProviders.SQLServer)),
            },
            connectionStrings,
            new EntryComparer());
        }

        [Fact]
        public void InvalidSelectObjectType()
        {
            var connectionStringsHelper = GetService();

            var result = Assert.ThrowsAny<InvalidOperationException>(() => connectionStringsHelper.ParseConnectionStrings(1));

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
        [InlineData(null, DbProviders.Custom, null)]
        [InlineData("", DbProviders.Custom, null)]
        [InlineData(" ", DbProviders.Custom, null)]
        [InlineData("sqlserver", DbProviders.SQLServer, null)]
        [InlineData("mssql", DbProviders.SQLServer, null)]
        [InlineData("sqlazure", DbProviders.SQLServer, null)]
        [InlineData("system.data.sqlclient", DbProviders.SQLServer, null)]
        [InlineData("Microsoft.Data.SqlClient", DbProviders.SQLServer, null)]
        [InlineData("mysql", DbProviders.MySQL, null)]
        [InlineData("mysql.data.mysqlclient", DbProviders.MySQL, null)]
        [InlineData("MySqlConnector", DbProviders.MySQL, null)]
        [InlineData("postgresql", DbProviders.PostgreSQL, null)]
        [InlineData("Npgsql", DbProviders.PostgreSQL, null)]
        [InlineData("Postgres", DbProviders.PostgreSQL, null)]
        [InlineData("cosmos", DbProviders.Custom, "cosmos")]
        [InlineData("Cosmos", DbProviders.Custom, "Cosmos")]
        [InlineData("COSMOS", DbProviders.Custom, "COSMOS")]
        [InlineData("Any", DbProviders.Custom, "Any")]
        public void ParseProviderNameTest(string providerName, DbProviders expectedProvider, string expectedCustom)
        {
            var (provider, custom) = ConnectionStringsHelper.ParseProviderName(providerName);

            Assert.Equal(expectedProvider, provider);
            Assert.Equal(expectedCustom, custom);
        }

        private class Entry
        {
            public Entry(string name, ConnectionStringInfo connStr)
            {
                Name = name;
                ConnStr = connStr;
            }
            public string Name { get; set; }
            public ConnectionStringInfo ConnStr { get; set; }
        }

        private class EntryComparer : IEqualityComparer<Entry>
        {
            public bool Equals([AllowNull] Entry x, [AllowNull] Entry y)
            {
                if (x == null || y == null)
                    return x == null && y == null;

                return string.Equals(x.Name, y.Name) &&
                    x.ConnStr is ConnectionStringInfo a &&
                    y.ConnStr is ConnectionStringInfo b &&
                    string.Equals(a.ConnectionString, b.ConnectionString) &&
                    a.Provider == b.Provider &&
                    string.Equals(a.Custom, b.Custom);
            }

            public int GetHashCode([DisallowNull] Entry obj)
                => obj.Name.GetHashCode() ^ (obj.ConnStr?.ConnectionString.GetHashCode() ?? 0) ^
                (obj.ConnStr?.Provider.GetHashCode() ?? 0) ^
                (obj.ConnStr?.Custom?.GetHashCode() ?? 0);
        }
    }
}
