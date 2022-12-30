using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class EnumBindTest
{
    private static IConfiguration NewConfig(Dictionary<string, string?> initialData)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

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
    public void ZeroDefault(string strValue, int expectedValue)
    {
        var config = NewConfig(new Dictionary<string, string?>
        {
            ["KeyStorageFlags"] = strValue,
        });

        var options = config.Get<MyOptions>();

        Assert.NotNull(options);
        Assert.True(options.KeyStorageFlags.HasValue);
        Assert.Equal(expectedValue, (int)options.KeyStorageFlags.Value);
    }

    internal class MyOptions
    {
        public X509KeyStorageFlags? KeyStorageFlags { get; set; }
    }
}
