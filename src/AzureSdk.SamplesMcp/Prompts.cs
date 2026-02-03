// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace AzureSdk.SamplesMcp;

[McpServerPromptType]
public static class Prompts
{
    [McpServerPrompt(Name = "explore_azure_sdk")]
    [Description("Explore Azure SDK dependencies and code samples in your project")]
    public static IEnumerable<ChatMessage> ExploreAzureSDK(
        [Description("The path to your project directory")] string projectPath = "."
    )
    {
        yield return new ChatMessage(
            ChatRole.User,
            $"""
Explore the Azure SDK dependencies and code samples in my project at {projectPath}.

First, help me understand:

1. What Azure SDK packages or libraries (crates) does my project use?
2. For each dependency, find and show me relevant code examples and documentation.
3. Explain what Azure services my project depends on (e.g., Key Vault, Storage, Identity, etc.).

Use the 'dependencies' tool to list packages, or the 'samples' tool to find examples for each one.
"""
        );
    }

    [McpServerPrompt(Name = "find_azure_sdk_samples")]
    [Description("Find code examples for a specific Azure SDK in your project")]
    public static IEnumerable<ChatMessage> FindAzureServiceSamples(
        [Description("The Azure service name (e.g., 'Key Vault', 'Storage', 'CosmosDB', 'Identity')")] string serviceName,
        [Description("The path to your project directory")] string projectPath = "."
    )
    {
        yield return new ChatMessage(
            ChatRole.User,
            $"""
Help me find and understand code examples for Azure {serviceName} SDK in my project at {projectPath}.

1. First, list the dependencies in my project using the 'dependencies' tool.
2. Identify which Azure SDK dependency corresponds to Azure {serviceName}.
3. Use the 'samples' tool to find code examples and documentation for that SDK.
4. Explain how to use the Azure {serviceName} SDK based on the examples found.

If my project doesn't use the Azure {serviceName} SDK, let me know what Azure SDKs it does use.
"""
        );
    }
}
