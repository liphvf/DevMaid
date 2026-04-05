using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevMaid.CLI.Commands;

/// <summary>
/// Provides commands to export, import, and list Windows optional features using DISM.
/// </summary>
public static class WindowsFeaturesCommand
{
    /// <summary>
    /// Builds the windowsfeatures command structure.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
    public static Command Build()
    {
        var command = new Command("windowsfeatures", "Export and import Windows optional features.");

        var exportCommand = new Command("export", "Export enabled Windows features to a file.");
        var exportPathArgument = new Argument<string>("path")
        {
            Description = "Path to save the exported features JSON file."
        };
        exportCommand.Add(exportPathArgument);
        exportCommand.SetAction(parseResult =>
        {
            var path = parseResult.GetValue(exportPathArgument)!;
            ExportFeatures(path);
        });

        var importCommand = new Command("import", "Import and enable Windows features from a file.");
        var importPathArgument = new Argument<string>("path")
        {
            Description = "Path to the JSON file containing exported features."
        };
        importCommand.Add(importPathArgument);
        importCommand.SetAction(parseResult =>
        {
            var path = parseResult.GetValue(importPathArgument)!;
            ImportFeatures(path);
        });

        var listEnabledOnlyOption = new Option<bool>("--enabled-only")
        {
            Description = "Show only enabled features."
        };

        var listCommand = new Command("list", "List all available Windows features.")
        {
            listEnabledOnlyOption
        };
        listCommand.SetAction(parseResult =>
        {
            var enabledOnly = parseResult.GetValue(listEnabledOnlyOption);
            ListFeatures(enabledOnly);
        });

        command.Add(exportCommand);
        command.Add(importCommand);
        command.Add(listCommand);

        return command;
    }

    /// <summary>
    /// Exports the currently enabled Windows features to a JSON file.
    /// </summary>
    /// <param name="outputPath">The path to write the exported features JSON file.</param>
    public static void ExportFeatures(string outputPath)
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

        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Exported {features.Count} enabled features to: {outputPath}");
    }

    /// <summary>
    /// Imports and enables Windows features from a previously exported JSON file.
    /// </summary>
    /// <param name="inputPath">The path to the JSON file containing the exported features.</param>
    public static void ImportFeatures(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: File not found: {inputPath}");
            return;
        }

        var json = File.ReadAllText(inputPath);
        var exportData = JsonSerializer.Deserialize<WindowsFeaturesExport>(json);

        if (exportData?.Features == null || exportData.Features.Count == 0)
        {
            Console.WriteLine("No features found in the import file.");
            return;
        }

        Console.WriteLine($"Importing {exportData.Features.Count} features...");
        var successCount = 0;
        var failCount = 0;

        foreach (var feature in exportData.Features)
        {
            Console.Write($"Enabling {feature}... ");
            var result = RunDismCommand($"/online /enable-feature /featurename:{feature} /All");
            if (result == 0)
            {
                Console.WriteLine("OK");
                successCount++;
            }
            else
            {
                Console.WriteLine($"FAILED (exit code: {result})");
                failCount++;
            }
        }

        Console.WriteLine($"\nImport complete. Success: {successCount}, Failed: {failCount}");
        if (failCount > 0)
        {
            Console.WriteLine("Some features may require a restart or elevated permissions.");
        }
    }

    /// <summary>
    /// Lists all available Windows features, optionally filtering to only enabled ones.
    /// </summary>
    /// <param name="enabledOnly">When <c>true</c>, shows only enabled features.</param>
    public static void ListFeatures(bool enabledOnly = false)
    {
        Console.WriteLine("Retrieving Windows features...");

        var result = RunDismCommand("/online /get-features /format:table", captureOutput: true);

        if (result != 0)
        {
            Console.WriteLine("Error retrieving features. Make sure you're running as Administrator.");
            return;
        }
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

    private static int RunDismCommand(string arguments, bool captureOutput = false)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = arguments,
                UseShellExecute = !captureOutput,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                CreateNoWindow = !captureOutput
            }
        };

        process.Start();

        if (captureOutput)
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }

            Console.WriteLine(output);
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}

/// <summary>
/// Represents the data exported from a Windows features snapshot.
/// </summary>
public class WindowsFeaturesExport
{
    /// <summary>Gets or sets the UTC timestamp when the export was created.</summary>
    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; }

    /// <summary>Gets or sets the list of enabled Windows feature names.</summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}
