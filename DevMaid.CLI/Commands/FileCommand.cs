using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;

using DevMaid.CommandOptions;

namespace DevMaid.Commands;

public static class FileCommand
{
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

    public static void Combine(FileCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            throw new ArgumentException("Input pattern is required.");
        }

        var pattern = Path.GetFileName(options.Input);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Input pattern is invalid.");
        }

        var directory = Path.GetDirectoryName(options.Input);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        // Validate directory path to prevent path traversal before normalizing
        if (!SecurityUtils.IsValidPath(directory))
        {
            throw new ArgumentException($"Invalid directory path: '{directory}'. Path traversal not allowed.");
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
            throw new ArgumentException($"Output path is outside the input directory: '{outputPath}'");
        }

        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        var inputFilePaths = Directory.GetFiles(fullDirectoryPath, pattern);
        Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

        if (!inputFilePaths.Any())
        {
            throw new Exception("Files not Found");
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
