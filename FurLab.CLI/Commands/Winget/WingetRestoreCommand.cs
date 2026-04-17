using System.Diagnostics;
using System.Text;

using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Winget;

/// <summary>
/// Restores winget packages from a backup file.
/// </summary>
public sealed class WingetRestoreCommand : AsyncCommand<WingetRestoreCommand.Settings>
{
    private const string BackupFileName = "backup-winget.json";

    /// <summary>
    /// Settings for the winget restore command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the input file path.
        /// </summary>
        [CommandOption("-i|--input")]
        [System.ComponentModel.Description("Input file (default: backup-winget.json in current directory)")]
        public string? Input { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var inputFile = settings.Input ?? Path.Combine(Environment.CurrentDirectory, BackupFileName);

        if (!System.IO.File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Backup file not found: {inputFile}");
            return Task.FromResult(2);
        }

        Console.WriteLine($"Starting winget import from: {inputFile}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = $"import -i \"{inputFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine(output);
        }

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"Error: {error}");
        }

        Console.WriteLine($"\nRestore completed with exit code: {process.ExitCode}");
        return Task.FromResult(process.ExitCode == 0 ? 0 : 1);
    }
}