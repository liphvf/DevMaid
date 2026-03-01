using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DevMaid.Commands;

public static class WingetCommand
{
    private const string BackupFileName = "backup-winget.json";

    public static Command Build()
    {
        var command = new Command("winget", "Manage winget packages.");

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output directory (default: current directory)"
        };

        var inputOption = new Option<string?>("--input", "-i")
        {
            Description = "Input file (default: backup-winget.json in current directory)"
        };

        var backupCommand = new Command("backup", "Backup installed winget packages.")
        {
            outputOption
        };

        backupCommand.SetAction(parseResult =>
        {
            var outputDir = parseResult.GetValue(outputOption);
            RunBackup(outputDir);
        });

        var restoreCommand = new Command("restore", "Restore winget packages from backup.")
        {
            inputOption
        };
        
        restoreCommand.SetAction(parseResult =>
        {
            var inputFile = parseResult.GetValue(inputOption);
            RunRestore(inputFile);
        });

        command.Add(backupCommand);
        command.Add(restoreCommand);

        return command;
    }

    private static void RunBackup(string? outputDir)
    {
        if (string.IsNullOrEmpty(outputDir))
        {
            outputDir = Directory.GetCurrentDirectory();
        }

        var backupPath = Path.Combine(outputDir, BackupFileName);

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
    }

    private static void RunRestore(string? inputFile)
    {
        if (string.IsNullOrEmpty(inputFile))
        {
            inputFile = Path.Combine(Directory.GetCurrentDirectory(), BackupFileName);
        }

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Backup file not found: {inputFile}");
            return;
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
    }
}
