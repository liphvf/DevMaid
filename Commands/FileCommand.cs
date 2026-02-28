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
        var command = new Command("combine", "Copy dashboards between databases.");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "Input Directory.",
            Required = true
        };
        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Input Directory."
        };

        command.Add(inputOption);
        command.Add(outputOption);

        command.SetAction(parseResult =>
        {
            var options = new FileCommandOptions
            {
                Input = parseResult.GetRequiredValue(inputOption),
                Output = parseResult.GetValue(outputOption)
            };

            Combine(options);
        });

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

        var extension = Path.GetExtension(options.Input);
        var outputPath = options.Output;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Join(directory, $"CombineFiles{extension}");
        }

        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        var inputFilePaths = Directory.GetFiles(directory, pattern);
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

        File.WriteAllText(outputPath, allFileText.ToString(), currentEncoding);
    }
}
