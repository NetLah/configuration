using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class EnumParseBindTest
{
    private static IConfiguration NewConfig(Dictionary<string, string?> initialData)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();
    }

    [Fact]
    public void Null()
    {
        var config = NewConfig(new Dictionary<string, string?>
        {
            ["NoThing"] = "Hello World!",
        });

        var options = config.Get<MyOptions>();

        Assert.NotNull(options);
        Assert.False(options.KeyStorageFlags.HasValue);
    }

    [Fact]
    public void Empty()
    {
        var config = NewConfig(new Dictionary<string, string?>
        {
            ["KeyStorageFlags"] = "",
        });

        var options = config.Get<MyOptions>();

        Assert.NotNull(options);
        Assert.False(options.KeyStorageFlags.HasValue);
    }

    // startAt: 0, count: 64
    public static IEnumerable<object[]> IntegerData
        => Enumerable.Range(0, 64).Select(i => new object[] { i.ToString(), i });

    [Theory]
    [MemberData(nameof(IntegerData))]
    [InlineData("DefaultKeySet", 0)]
    [InlineData("UserKeySet", 1)]
    [InlineData("MachineKeySet", 2)]
    [InlineData("Exportable", 4)]
    [InlineData("UserProtected", 8)]
    [InlineData("PersistKeySet", 16)]
    [InlineData("EphemeralKeySet", 32)]
    [InlineData("UserKeySet,MachineKeySet", 3)]
    [InlineData("DefaultKeySet,UserKeySet,MachineKeySet", 3)]
    [InlineData("MachineKeySet,UserKeySet", 3)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected", 15)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected,UserProtected", 15)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected,PersistKeySet,EphemeralKeySet", 63)]
    public void ConfigurationBind(string input, int expectedValue)
    {
        var config = NewConfig(new Dictionary<string, string?>
        {
            ["KeyStorageFlags"] = input,
        });

        var options = config.Get<MyOptions>();

        Assert.NotNull(options);
        Assert.True(options.KeyStorageFlags.HasValue);
        Assert.Equal(expectedValue, (int)options.KeyStorageFlags.Value);
    }

    [Theory]
    [MemberData(nameof(IntegerData))]
    [InlineData("DefaultKeySet", 0)]
    [InlineData("UserKeySet", 1)]
    [InlineData("MachineKeySet", 2)]
    [InlineData("Exportable", 4)]
    [InlineData("UserProtected", 8)]
    [InlineData("PersistKeySet", 16)]
    [InlineData("EphemeralKeySet", 32)]
    [InlineData("UserKeySet,MachineKeySet", 3)]
    [InlineData("DefaultKeySet,UserKeySet,MachineKeySet", 3)]
    [InlineData("MachineKeySet,UserKeySet", 3)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected", 15)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected,UserProtected", 15)]
    [InlineData("UserKeySet,MachineKeySet,Exportable,UserProtected,PersistKeySet,EphemeralKeySet", 63)]
    public void EnumParse(string input, int expectedValue)
    {
        if (Enum.TryParse<X509KeyStorageFlags>(input, out var value))
        {
            Assert.Equal(expectedValue, (int)value);
        }
        else
        {
            Assert.Fail("Cannot parse: " + input);
        }
    }

    internal class MyOptions
    {
        public X509KeyStorageFlags? KeyStorageFlags { get; set; }
    }
}
