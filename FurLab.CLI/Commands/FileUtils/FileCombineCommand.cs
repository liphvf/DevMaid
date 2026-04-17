using System.Text;

using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.FileUtils;

/// <summary>
/// Combines files matching a pattern into a single output file.
/// </summary>
public sealed class FileCombineCommand : AsyncCommand<FileCombineCommand.Settings>
{
    /// <summary>
    /// Settings for the file combine command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the input file pattern (e.g., *.sql, *.txt).
        /// </summary>
        [CommandOption("-i|--input")]
        [System.ComponentModel.Description("Input file pattern (e.g., *.sql, *.txt).")]
        public string Input { get; init; } = string.Empty;

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        [CommandOption("-o|--output")]
        [System.ComponentModel.Description("Output file path.")]
        public string? Output { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(settings.Input))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Input pattern is required. Use -i/--input to specify a file pattern.");
            return Task.FromResult(2);
        }

        var pattern = Path.GetFileName(settings.Input);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Input pattern is invalid.");
            return Task.FromResult(2);
        }

        var directory = Path.GetDirectoryName(settings.Input);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Environment.CurrentDirectory;
        }

        if (!SecurityUtils.IsValidPath(directory))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid directory path: '{directory}'. Path traversal not allowed.");
            return Task.FromResult(2);
        }

        var fullDirectoryPath = Path.GetFullPath(directory);
        var extension = Path.GetExtension(settings.Input);
        var outputPath = settings.Output;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(directory, $"CombineFiles{extension}");
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        if (!SecurityUtils.IsValidPath(fullOutputPath, fullDirectoryPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Output path is outside the input directory: '{outputPath}'");
            return Task.FromResult(2);
        }

        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        var inputFilePaths = System.IO.Directory.GetFiles(fullDirectoryPath, pattern);
        Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

        if (inputFilePaths.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No files found matching pattern '{pattern}' in directory '{fullDirectoryPath}'.");
            return Task.FromResult(2);
        }

        foreach (var inputFilePath in inputFilePaths)
        {
            currentEncoding = FurLab.CLI.Utils.Utils.GetCurrentFileEncoding(inputFilePath);
            allFileText.Append(System.IO.File.ReadAllText(inputFilePath, currentEncoding));
            allFileText.AppendLine();

            Console.WriteLine("The file {0} has been processed.", inputFilePath);
        }

        System.IO.File.WriteAllText(fullOutputPath, allFileText.ToString(), currentEncoding);
        return Task.FromResult(0);
    }
}