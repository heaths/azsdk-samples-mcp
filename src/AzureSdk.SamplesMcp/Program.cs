// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AzureSdk.SamplesMcp;
using AzureSdk.SamplesMcp.Services;
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
    .AddSingleton<IExternalProcessService, ExternalProcessService>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();
await builder.Build().RunAsync();
