using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevMaid.Commands;

public static class WindowsFeaturesCommand
{
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

public class WindowsFeaturesExport
{
    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; }

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}
