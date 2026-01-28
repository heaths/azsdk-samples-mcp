using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp.Services;

internal class Command(string name, ILogger? logger) : IDisposable
{
    private Process? _process;

    public string Name => name;

    public List<string> Arguments { get; } = [];

    public string StandardError { get; private set; } = string.Empty;

    public string StandardOutput { get; private set; } = string.Empty;

    public string? WorkingDirectory { get; set; } = Environment.CurrentDirectory;

    public void Dispose() => _process?.Dispose();

    public async Task<int> Run(CancellationToken cancellationToken = default)
    {
        ProcessStartInfo info = new(name, Arguments)
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = WorkingDirectory,
        };
        logger?.LogDebug("Running: {} {}", info.FileName, string.Join(" ", info.ArgumentList));

        _process = Process.Start(info) ?? throw new Exception($"Failed to launch {name}");

        // Capture stdout and stderr asynchronously
        Task<string> stdoutTask = _process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = _process.StandardError.ReadToEndAsync(cancellationToken);

        await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        // Get the captured output
        StandardOutput = await stdoutTask.ConfigureAwait(false);
        StandardError = await stderrTask.ConfigureAwait(false);

        logger?.LogDebug("Process exited with code {}", _process.ExitCode);
        return _process.ExitCode;
    }
}
