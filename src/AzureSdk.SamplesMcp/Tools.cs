// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.ComponentModel;
using AzureSdk.SamplesMcp.Providers;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerToolType]
public static class Tools
{
    private static readonly IDependencyProvider[] s_providers = [
        new Cargo(),
        new Dotnet(),
        new Node(),
    ];

    [McpServerTool(Name = "dependencies")]
    [Description("Discover Azure SDK dependencies (packages or crates) in your .NET, Node.js, or Rust project. Identifies which Azure services (Key Vault, Storage, CosmosDB, Identity, etc.) your project uses. Supports .csproj, package.json, and Cargo.toml files.")]
    public static async Task<IEnumerable<string>> GetDependencies(
        RequestContext<CallToolRequestParams> context,
        [Description("The path where to start looking for project or manifest files")] string path
    )
    {
        ILogger logger = context.Services!.GetRequiredService<ILoggerFactory>().CreateLogger("Dependencies");
        IExternalProcessService processService = context.Services!.GetRequiredService<IExternalProcessService>();
        FileSystem fileSystem = context.Services?.GetService<FileSystem>() ?? FileSystem.Default;

        if (fileSystem.FileExists(path))
        {
            path = fileSystem.GetParent(path) ?? throw new McpException($"Require directory for {path}");
        }

        logger.LogDebug("Looking for dependencies in directory {}", path);

        (string Directory, IDependencyProvider Provider)? result = fileSystem.FindProvider(path, s_providers);
        if (result is not ({ } directory, { } provider))
            return [];
        logger.LogDebug("Found provider {} for directory {}", provider.GetType().Name, directory);
        IEnumerable<Dependency> dependencies = await provider.GetDependencies(directory, processService, logger, fileSystem, includeDescriptions: true);
        return dependencies.Select(d => 
        {
            if (string.IsNullOrWhiteSpace(d.Description))
            {
                return d.Name;
            }
            return $"{d.Name}: {d.Description}";
        });
    }

    [McpServerTool(Name = "samples")]
    [Description("Find code examples and documentation for Azure SDK libraries in your project. Retrieves README files and sample code for services like Azure Key Vault, Azure Storage, Azure Identity, CosmosDB, and more. Use this to quickly learn how to use the Azure services your project depends on.")]
    public static async Task<IEnumerable<ContentBlock>> GetSamples(
        RequestContext<CallToolRequestParams> context,
        [Description("The path where to start looking for project or manifest files")] string path,
        [Description("A specific dependency from which samples are retrieved")] string? dependency = null
    )
    {
        ILogger logger = context.Services!.GetRequiredService<ILoggerFactory>().CreateLogger("Samples");
        IExternalProcessService processService = context.Services!.GetRequiredService<IExternalProcessService>();
        FileSystem fileSystem = context.Services?.GetService<FileSystem>() ?? FileSystem.Default;

        if (fileSystem.FileExists(path))
        {
            path = fileSystem.GetParent(path) ?? throw new McpException($"Require directory for {path}");
        }

        logger.LogDebug("Looking for samples in directory {}", path);

        (string Directory, IDependencyProvider Provider)? result = fileSystem.FindProvider(path, s_providers);
        if (result is not ({ } directory, { } provider))
            return [];
        logger.LogDebug("Found provider {} for directory {}", provider.GetType().Name, directory);

        IEnumerable<Dependency> dependencies = await provider.GetDependencies(directory, processService, logger);

        // Filter by dependency parameter if provided
        if (!string.IsNullOrWhiteSpace(dependency))
        {
            dependencies = dependencies.Where(d => string.Equals(d.Name, dependency, StringComparison.OrdinalIgnoreCase));
        }

        IEnumerable<string> samples = await provider.GetSamples(directory, dependencies, processService, logger);
        return samples.Select(samplePath =>
        {
            // cspell:ignore dylo
            //
            // BUG: The URI is correct, but the MCP server returns back something like '/file/dylo78gyp/Users/heaths/.cargo/registry/src/index.crates.io-1949cf8c6b5b557f/azure_core-0.30.1/README.md'
            // while VSCode interprets that as 'mcp-resource://6d63702e636f6e6669672e7773302e617a73646b2d73616d706c65732d6d6370/file/dylo78gyp/Users/heaths/.cargo/registry/src/index.crates.io-1949cf8c6b5b557f/azure_core-0.30.1/README.md'.
            var uri = new Uri("file://" + samplePath).AbsoluteUri;

            var text = fileSystem.ReadAllText(samplePath);

            // return new ResourceLinkBlock
            // {
            //     Uri = uri,
            //     Name = Path.GetFileName(samplePath),
            //     MimeType = "text/plain",
            // };
            return new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents
                {
                    Uri = uri,
                    Text = text,
                    MimeType = "text/plain",
                },
            };
        });
    }
}
