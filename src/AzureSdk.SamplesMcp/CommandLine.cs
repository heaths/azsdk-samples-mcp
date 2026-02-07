// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace AzureSdk.SamplesMcp;

internal static class CommandLine
{
    public static RootCommand Build()
    {
        RootCommand rootCommand = new("Azure SDK Samples MCP Server - discover and retrieve code samples from Azure SDK dependencies");

        // Default action: start the MCP server
        rootCommand.SetAction(async parseResult =>
        {
            await Program.StartMcpServerAsync();
            return 0;
        });

        // Add config subcommand
        Command configCommand = new("config", "Generate MCP configuration files for AI assistants");

        Argument<string> targetArgument = new("target")
        {
            Description = "Target AI assistant (copilot, claude, or vscode)"
        };

        Option<bool> globalOption = new("--global")
        {
            Description = "Create global configuration instead of local (repository-level)"
        };
        globalOption.Aliases.Add("-g");

        configCommand.Arguments.Add(targetArgument);
        configCommand.Options.Add(globalOption);

        configCommand.SetAction(async parseResult =>
        {
            string? target = parseResult.GetValue(targetArgument);
            bool isGlobal = parseResult.GetValue(globalOption);
            if (target is null)
            {
                return 1;
            }
            return await ConfigCommandHandler.HandleAsync(target, isGlobal);
        });

        rootCommand.Subcommands.Add(configCommand);

        return rootCommand;
    }
}
