using System.CommandLine;
using System.Text;

using FurLab.CLI.CommandOptions;

using Spectre.Console;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides file utility commands such as combining multiple files.
/// </summary>
public static class FileCommand
{
    /// <summary>
    /// Builds the file command structure.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
    public static Command Build()
    {
        var command = new Command("file", "File utilities.");

        var combineCommand = new Command("combine", "Combine files in a directory into a single file.");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "Input file pattern (e.g., *.sql, *.txt).",
            Required = true
        };
        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path."
        };

        combineCommand.Add(inputOption);
        combineCommand.Add(outputOption);

        combineCommand.SetAction(parseResult =>
        {
            var options = new FileCommandOptions
            {
                Input = parseResult.GetRequiredValue(inputOption),
                Output = parseResult.GetValue(outputOption)
            };

            Combine(options);
        });

        command.Add(combineCommand);

        return command;
    }

    /// <summary>
    /// Combines all files matching the input pattern into a single output file.
    /// </summary>
    /// <param name="options">The file command options specifying input pattern and output path.</param>
    public static void Combine(FileCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Input pattern is required. Use -i/--input to specify a file pattern.");
            Environment.Exit(2);
            return;
        }

        var pattern = Path.GetFileName(options.Input);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Input pattern is invalid.");
            Environment.Exit(2);
            return;
        }

        var directory = Path.GetDirectoryName(options.Input);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        // Validate directory path to prevent path traversal before normalizing
        if (!SecurityUtils.IsValidPath(directory))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid directory path: '{directory}'. Path traversal not allowed.");
            Environment.Exit(2);
            return;
        }

        var fullDirectoryPath = Path.GetFullPath(directory);

        var extension = Path.GetExtension(options.Input);
        var outputPath = options.Output;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(directory, $"CombineFiles{extension}");
        }

        // Validate output path to prevent path traversal
        var fullOutputPath = Path.GetFullPath(outputPath);
        if (!SecurityUtils.IsValidPath(fullOutputPath, fullDirectoryPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Output path is outside the input directory: '{outputPath}'");
            Environment.Exit(2);
            return;
        }

        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        var inputFilePaths = Directory.GetFiles(fullDirectoryPath, pattern);
        Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

        if (!inputFilePaths.Any())
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No files found matching pattern '{pattern}' in directory '{fullDirectoryPath}'.");
            Environment.Exit(2);
            return;
        }

        foreach (var inputFilePath in inputFilePaths)
        {
            currentEncoding = Utils.GetCurrentFileEncoding(inputFilePath);
            allFileText.Append(File.ReadAllText(inputFilePath, currentEncoding));
            allFileText.AppendLine();

            Console.WriteLine("The file {0} has been processed.", inputFilePath);
        }

        File.WriteAllText(fullOutputPath, allFileText.ToString(), currentEncoding);
    }
}
