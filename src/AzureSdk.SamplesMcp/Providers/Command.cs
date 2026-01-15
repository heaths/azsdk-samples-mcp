using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp.Providers;

class Command(string name, ILogger? logger) : IDisposable
{
    Process? process;

    public string Name => name;

    public List<string> Arguments { get; } = [];

    public string StandardError { get; private set; } = string.Empty;

    public string StandardOutput { get; private set; } = string.Empty;

    public string? WorkingDirectory { get; set; } = Environment.CurrentDirectory;

    public void Dispose() => process?.Dispose();

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

        process = Process.Start(info) ?? throw new Exception($"Failed to launch {name}");

        // Capture stdout and stderr asynchronously
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        // Get the captured output
        StandardOutput = await stdoutTask.ConfigureAwait(false);
        StandardError = await stderrTask.ConfigureAwait(false);

        logger?.LogDebug("Process exited with code {}", process.ExitCode);
        return process.ExitCode;
    }
}
