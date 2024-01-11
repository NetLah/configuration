using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class ConnectionStringParserTest
{
    private static readonly Regex QuoteTokenRegex = ConnectionStringParser.QuoteTokenRegex;

    private static ConnectionStringParser GetService(ProviderName? select)
    {
        var configuration = new KeyValuePair<string, string>[] {
                new("default1", "Default1"),
                new("conn2_sqlServer", "sqlServer2"),
                new("conn3_PostgreSQL", "postgreSQL3"),
                new("conn4_MySQL", "mySql4"),
                new("conn5_Cosmos", "cosmos5"),
                new("conn6", "sqlServer6"),
                new("conn6_ProviderName", "sqlServer"),
                new("conn7", "postgreSQL7"),
                new("conn7_PROVIDERNAME", "POSTGRESQL"),
                new("conn8", "mySql8"),
                new("conn8_providername", "mysql"),
                new("conn9", "cosmos9"),
                new("conn9_providerName", "COSmos"),
            };
        return new ConnectionStringParser(configuration, select, ConnectionStringsRoot.KeyTrimNormalizer);
    }

    [Fact]
    public void ExpandValue_Test()
    {
        var list = new List<ProviderConnectionString> {
                new("conn1", "value1;r2=${conn2};{conn2};r3=$(conn3);${conn1}"),
                new("conn2", "value2;ref4a=$[conn4];"),
                new("conn3", "value3;%{conn2};ref4b=%(conn4);"),
                new("conn4", "value4;"),
            };
        var service = new ConnectionStringParser(Enumerable.Empty<KeyValuePair<string, string>>(), null, ConnectionStringsRoot.KeyTrimNormalizer);
        var result = service.ExpandValue(list);

        Assert.Equal(new[] {
                "conn4",
                "conn2",
                "conn3",
                "conn1",
            }, result.Keys);

        Assert.Equal(new[] {
                "conn4: value4;",
                "conn2: value2;ref4a=value4;;",
                "conn3: value3;value2;ref4a=value4;;;ref4b=value4;;",
                "conn1: value1;r2=value2;ref4a=value4;;;{conn2};r3=value3;value2;ref4a=value4;;;ref4b=value4;;;${conn1}",
            }, result.Values.Select(c => $"{c.Name}: {c.Value}"));
    }

    [Fact]
    public void ListConnections_All()
    {
        var service = GetService(null);
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn6", "sqlServer6", DbProviders.SQLServer),
                new ProviderConnectionString("conn7", "postgreSQL7", DbProviders.PostgreSQL),
                new ProviderConnectionString("conn8", "mySql8", DbProviders.MySQL),
                new ProviderConnectionString("conn9", "cosmos9", DbProviders.Custom, "COSmos"),
                new ProviderConnectionString("default1", "Default1"),
                new ProviderConnectionString("conn4", "mySql4", DbProviders.MySQL),
                new ProviderConnectionString("conn5", "cosmos5", DbProviders.Custom, "Cosmos"),
                new ProviderConnectionString("conn5_Cosmos", "cosmos5", DbProviders.Custom),
                new ProviderConnectionString("conn2", "sqlServer2", DbProviders.SQLServer),
                new ProviderConnectionString("conn3", "postgreSQL3", DbProviders.PostgreSQL),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void ListConnections_Cosmos()
    {
        var service = GetService(new ProviderName("COSMOS"));
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn9", "cosmos9", DbProviders.Custom, "COSmos"),
                new ProviderConnectionString("conn5", "cosmos5", DbProviders.Custom, "Cosmos"),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void ListConnections_Custom()
    {
        var service = GetService(new ProviderName(DbProviders.Custom));
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn9", "cosmos9", DbProviders.Custom, "COSmos"),
                new ProviderConnectionString("default1", "Default1"),
                new ProviderConnectionString("conn5", "cosmos5", DbProviders.Custom, "Cosmos"),
                new ProviderConnectionString("conn5_Cosmos", "cosmos5", DbProviders.Custom),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void ListConnections_MySql()
    {
        var service = GetService(new ProviderName(DbProviders.MySQL));
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn8", "mySql8", DbProviders.MySQL),
                new ProviderConnectionString("conn4", "mySql4", DbProviders.MySQL),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void ListConnections_PostgreSQL()
    {
        var service = GetService(new ProviderName(DbProviders.PostgreSQL));
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn7", "postgreSQL7", DbProviders.PostgreSQL),
                new ProviderConnectionString("conn3", "postgreSQL3", DbProviders.PostgreSQL),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void ListConnections_SqlServer()
    {
        var service = GetService(new ProviderName(DbProviders.SQLServer));
        var result = service.ListConnections();

        Assert.Equal(new[] {
                new ProviderConnectionString("conn6", "sqlServer6", DbProviders.SQLServer),
                new ProviderConnectionString("conn2", "sqlServer2", DbProviders.SQLServer),
            }, result, ProviderConnectionStringComparer.Instance);
    }

    [Fact]
    public void NewEntryTest()
    {
        var connStrValue = "Key1=Value1; $[ TOKEN2 ];${token1};$( token2 ); $[token3]; %{token4}; %(token5); %[token6];";
        var connStr = new ProviderConnectionString("name1", connStrValue);

        var service = new ConnectionStringParser(Enumerable.Empty<KeyValuePair<string, string>>(),
            null,
            ConnectionStringsRoot.KeyTrimNormalizer);

        var result = service.NewEntry(connStr);

        Assert.NotNull(result);
        Assert.Equal("name1", result.Name);
        Assert.Equal(connStrValue, result.Raw);
        Assert.False(result.HasQuotes);
        Assert.Equal(new string[] { "TOKEN2", "token1", "token3", "token4", "token5", "token6" }, result.Tokens);
        Assert.Equal(0, result.Remaining);
    }

    [Theory]
    [InlineData("Key1=Value1;$$", true)]
    [InlineData("Key1=Value1;%%", true)]
    [InlineData("Key1=Value1;$%", false)]
    [InlineData("Key1=Value1;%$", false)]
    public void NewEntryTest_HasQuotes(string connStrValue, bool expected)
    {
        var connStr = new ProviderConnectionString("name1", connStrValue);
        var service = new ConnectionStringParser(Enumerable.Empty<KeyValuePair<string, string>>(),
            null,
            ConnectionStringsRoot.KeyTrimNormalizer);

        var result = service.NewEntry(connStr);

        Assert.Equal(expected, result.HasQuotes);
    }

    [Fact]
    public void ParseConfigurationKeyValueTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["key2"] = "Value2",
                ["complex:a"] = "C1",
                ["array:0"] = "A1",
                ["array:1"] = "C1",
                ["key1"] = "value1",
            })
            .Build();

        var result = ConnectionStringParser.ParseConfigurationKeyValue(configuration);

        Assert.Equal(new[] {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "Value2"),
            }, result);
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
    public void ParseProviderNameTest(string? providerName, DbProviders expectedProvider, string? expectedCustom)
    {
        var provider = ConnectionStringParser.ParseProviderName(providerName);
        Assert.Equal(expectedProvider, provider.Provider);
        Assert.Equal(expectedCustom, provider.Custom);
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("a", "a", false)]
    [InlineData("$a", "$a", false)]
    [InlineData("$$a", "$$a", true)]
    [InlineData("$${a}", "$${a}", true)]
    [InlineData("${a}", "a", true)]
    [InlineData("1${a}", "1a", true)]
    [InlineData("${a}2", "a2", true)]
    [InlineData("1${a}2", "1a2", true)]
    [InlineData("1${a}2${b}", "1a2b", true)]
    [InlineData("1${a}${b}", "1ab", true)]
    [InlineData("${a}${bb}", "abb", true)]
    [InlineData("$${a}${bb}", "$${a}bb", true)]
    [InlineData("${a}$${bb}", "a$${bb}", true)]
    [InlineData("$$${a}", "$$a", true)]
    public void Regex_Test(string input, string expected, bool isMatchedExpected)
    {
        var replacement = "${quote}${token}";

        var isMatched = QuoteTokenRegex.IsMatch(input);
        var result = QuoteTokenRegex.Replace(input, replacement);

        Assert.Equal(isMatchedExpected, isMatched);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RegexNullInputTest()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var ex = Assert.Throws<ArgumentNullException>(() => QuoteTokenRegex.Replace(null, ""));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        Assert.Equal("input", ex.ParamName);
    }
}
