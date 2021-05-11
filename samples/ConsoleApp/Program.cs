using System;
using Microsoft.Extensions.Configuration;
using NetLah.Extensions.Configuration;

namespace ConsoleApp
{
#pragma warning disable S1118 // Utility classes should not have public constructors
    internal class Program
#pragma warning restore S1118 // Utility classes should not have public constructors
    {
        public static void Main(string[] args)
        {
            var configuration = ConfigurationBuilderBuilder.Create<Program>(args).Build();
            var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine($"[TRACE] ConnectionString: {defaultConnectionString}");
        }
    }
}
