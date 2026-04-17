using System.Diagnostics;

using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.WindowsFeatures;

/// <summary>
/// Lists all available Windows features, optionally filtering to enabled only.
/// </summary>
public sealed class WindowsFeaturesListCommand : AsyncCommand<WindowsFeaturesListCommand.Settings>
{
    /// <summary>
    /// Settings for the windowsfeatures list command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets a value indicating whether to show only enabled features.
        /// </summary>
        [CommandOption("--enabled-only")]
        [System.ComponentModel.Description("Show only enabled features.")]
        public bool EnabledOnly { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        Console.WriteLine("Retrieving Windows features...");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = "/online /get-features /format:table",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }

            Console.WriteLine("Error retrieving features. Make sure you're running as Administrator.");
            return Task.FromResult(1);
        }

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine(error);
        }

        Console.WriteLine(output);

        return Task.FromResult(0);
    }
}
