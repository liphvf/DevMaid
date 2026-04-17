using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Clean;

/// <summary>
/// Removes bin and obj folders from a solution directory.
/// </summary>
public sealed class CleanCommand : AsyncCommand<CleanCommand.Settings>
{
    /// <summary>
    /// Settings for the clean command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the directory path to clean. Uses current directory if not specified.
        /// </summary>
        [CommandArgument(0, "[directory]")]
        [System.ComponentModel.Description("Directory path to clean. If not specified, uses current directory.")]
        public string? Directory { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var path = settings.Directory ?? Environment.CurrentDirectory;
        var directory = File.Exists(path)
            ? Path.GetDirectoryName(path)
            : path;

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Environment.CurrentDirectory;
        }

        directory = Path.GetFullPath(directory);

        Console.WriteLine($"Cleaning directory: {directory}");

        var removedCount = 0;
        var directories = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);

        foreach (var dir in directories)
        {
            var dirName = Path.GetFileName(dir);
            if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.Delete(dir, true);
                    Console.WriteLine($"Removed: {dir}");
                    removedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing {dir}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"Cleaned {removedCount} folders.");
        return Task.FromResult(0);
    }
}
