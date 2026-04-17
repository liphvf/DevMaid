using System.Diagnostics;
using System.Text;

using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Winget;

/// <summary>
/// Backs up installed winget packages.
/// </summary>
public sealed class WingetBackupCommand : AsyncCommand<WingetBackupCommand.Settings>
{
    /// <summary>
    /// Settings for the winget backup command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the output directory path.
        /// </summary>
        [CommandOption("-o|--output")]
        [System.ComponentModel.Description("Output directory (default: current directory)")]
        public string? Output { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var outputDir = settings.Output ?? Environment.CurrentDirectory;
        var backupPath = Path.Combine(outputDir, "backup-winget.json");

        Console.WriteLine($"Starting winget export to: {backupPath}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = $"export -o \"{backupPath}\" --source winget",
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

        if (process.ExitCode == 0 && File.Exists(backupPath))
        {
            Console.WriteLine($"\nBackup completed successfully!");
            Console.WriteLine($"File: {backupPath}");
        }
        else
        {
            Console.WriteLine($"\nBackup failed with exit code: {process.ExitCode}");
        }

        return Task.FromResult(process.ExitCode == 0 ? 0 : 1);
    }
}
