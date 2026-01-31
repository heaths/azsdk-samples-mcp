using System.Text.Json;
using AzureSdk.SamplesMcp.Services;

namespace AzureSdk.SamplesMcp.Test.Services;

internal class MockProcessService : IExternalProcessService
{
    private readonly string _output;
    private readonly int _exitCode;

    public MockProcessService(string output, int exitCode = 0)
    {
        _output = output;
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
        var result = new ProcessResult(_exitCode, _output, string.Empty, $"{executablePath} {arguments}");
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
