// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Text.Json;

namespace AzureSdk.SamplesMcp.Services;

internal class MockMultiProcessService : IExternalProcessService
{
    private readonly Dictionary<string, string> _outputs;
    private readonly int _exitCode;

    public MockMultiProcessService(Dictionary<string, string> outputs, int exitCode = 0)
    {
        _outputs = outputs;
        _exitCode = exitCode;
    }

    public Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = default,
        int operationTimeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var command = $"{executablePath} {arguments}";
        var output = _outputs.TryGetValue(command, out var value) ? value : string.Empty;
        var result = new ProcessResult(_exitCode, output, string.Empty, command);
        return Task.FromResult(result);
    }

    public JsonElement ParseJsonOutput(ProcessResult result)
    {
        if (result.ExitCode != 0)
        {
            return JsonDocument.Parse("{}").RootElement;
        }

        using var jsonDocument = JsonDocument.Parse(result.Output);
        return jsonDocument.RootElement.Clone();
    }
}
