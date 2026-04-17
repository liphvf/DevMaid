using System.Diagnostics;
using System.Text.Json;

using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.WindowsFeatures;

/// <summary>
/// Exports enabled Windows features to a JSON file.
/// </summary>
public sealed class WindowsFeaturesExportCommand : AsyncCommand<WindowsFeaturesExportCommand.Settings>
{
    /// <summary>
    /// Settings for the windowsfeatures export command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the path to save the exported features JSON file.
        /// </summary>
        [CommandArgument(0, "<path>")]
        [System.ComponentModel.Description("Path to save the exported features JSON file.")]
        public string Path { get; init; } = string.Empty;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        Console.WriteLine("Getting enabled Windows features...");

        var features = GetEnabledFeatures();

        var exportData = new WindowsFeaturesExport
        {
            ExportedAt = DateTime.UtcNow,
            Features = features
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(settings.Path, json);
        Console.WriteLine($"Exported {features.Count} enabled features to: {settings.Path}");

        return Task.FromResult(0);
    }

    private static List<string> GetEnabledFeatures()
    {
        var enabledFeatures = new List<string>();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = "/online /get-features /format:list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Enabled"))
            {
                var featureName = ExtractFeatureName(line);
                if (!string.IsNullOrEmpty(featureName))
                {
                    enabledFeatures.Add(featureName);
                }
            }
        }

        return enabledFeatures;
    }

    private static string? ExtractFeatureName(string line)
    {
        var startIndex = line.IndexOf("Feature Name :", StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            startIndex += "Feature Name :".Length;
            var endIndex = line.IndexOf('\r', startIndex);
            if (endIndex < 0)
            {
                endIndex = line.Length;
            }
            return line.Substring(startIndex, endIndex - startIndex).Trim();
        }
        return null;
    }
}
