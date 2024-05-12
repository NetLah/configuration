using Microsoft.Extensions.Configuration;
using NetLah.Diagnostics;
using NetLah.Extensions.Configuration;
using NetLah.Extensions.Logging;

AppLog.InitLogger();
var appInfo = ApplicationInfo.Initialize(null);
Console.WriteLine($"AppTitle: {appInfo.Title}");
Console.WriteLine($"Version:{appInfo.InformationalVersion} BuildTime:{appInfo.BuildTimestampLocal}; Framework:{appInfo.FrameworkName}");

var asmAbstracts = new AssemblyInfo(typeof(AssemblyInfo).Assembly);
Console.WriteLine($"AssemblyTitle: {asmAbstracts.Title}");
Console.WriteLine($"Version:{asmAbstracts.InformationalVersion} BuildTime:{asmAbstracts.BuildTimestampLocal}; Framework:{asmAbstracts.FrameworkName}");

var asmConfig = new AssemblyInfo(typeof(ConfigurationBuilderBuilder).Assembly);
Console.WriteLine($"AssemblyTitle: {asmConfig.Title}");
Console.WriteLine($"Version:{asmConfig.InformationalVersion} BuildTime:{asmConfig.BuildTimestampLocal}; Framework:{asmConfig.FrameworkName}");

var asmLib = new AssemblyInfo(typeof(ConfigurationBinder).Assembly);
Console.WriteLine($"AssemblyTitle: {asmLib.Title}");
Console.WriteLine($"Version:{asmLib.InformationalVersion} BuildTime:{asmLib.BuildTimestampLocal}; Framework:{asmLib.FrameworkName}");

#if NET6_0_OR_GREATER
var configuration = ConfigurationBuilderBuilder.Create<Program>(args).Manager.AddConfigurationSource().AddTransformConfiguration();
#else
var configuration = ConfigurationBuilderBuilder.Create<Program>(args).WithConfigurationSource().WithTransformConfiguration().Build();
#endif
var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[TRACE] ConnectionString: {defaultConnectionString}");

var serilogKey = "Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Authentication";
var serilogValue = configuration[serilogKey];
Console.WriteLine($"[TRACE] {serilogKey} = {serilogValue}");
Console.WriteLine($"[TRACE] Serilog:MinimumLevel:Override:NetLah.Extensions.Configuration = {configuration["Serilog:MinimumLevel:Override:NetLah.Extensions.Configuration"]}");
