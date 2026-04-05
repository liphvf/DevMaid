using System.CommandLine;

namespace DevMaid.CLI.Commands;

/// <summary>
/// Provides a command to remove bin and obj build output folders.
/// </summary>
public static class CleanCommand
{
    /// <summary>
    /// Builds the clean command structure.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
    public static Command Build()
    {
        var command = new Command("clean", "Remove bin and obj folders from solution.");

        var directoryArgument = new Argument<string?>("directory")
        {
            Description = "Directory path to clean. If not specified, uses current directory."
        };

        command.Add(directoryArgument);

        command.SetAction(parseResult =>
        {
            var directoryPath = parseResult.GetValue(directoryArgument);
            Clean(directoryPath);
        });

        return command;
    }

    /// <summary>
    /// Removes all bin and obj directories under the specified path.
    /// </summary>
    /// <param name="directoryPath">The root directory to clean. Uses current directory if null.</param>
    public static void Clean(string? directoryPath)
    {
        var path = directoryPath ?? Directory.GetCurrentDirectory();
        var directory = File.Exists(path)
            ? Path.GetDirectoryName(path)
            : path;

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
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
    }
}
