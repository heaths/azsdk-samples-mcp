using AzureSdk.SamplesMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Debug;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Services
    .AddSingleton(DefaultEnvironment.Default)
    .AddSingleton(FileSystem.Default)
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();
await builder.Build().RunAsync();
