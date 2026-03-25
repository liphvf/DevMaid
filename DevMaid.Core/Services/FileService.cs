using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Models;

namespace DevMaid.Core.Services;

/// <summary>
/// Provides methods for file operations including combining files.
/// </summary>
public class FileService : IFileService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FileService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Combines multiple files into a single output file.
    /// </summary>
    /// <param name="options">The combine options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the combine operation with the result.</returns>
    public async Task<FileCombineResult> CombineFilesAsync(
        FileCombineOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Get input files
            var inputFiles = GetInputFiles(options);
            if (inputFiles.Count == 0)
            {
                return FileCombineResult.FailureResult(
                    "No input files found",
                    duration: DateTime.UtcNow - startTime);
            }

            // Determine output path
            var outputPath = options.Output;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = "combined.txt";
            }

            EnsureDirectoryExists(outputPath);

            // Combine files
            var totalSize = 0L;
            var separator = options.Separator ?? string.Empty;

            progress?.Report(OperationProgress.Create(0, inputFiles.Count, "Starting file combination..."));

            using (var outputStream = new StreamWriter(outputPath))
            {
                for (int i = 0; i < inputFiles.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var file = inputFiles[i];
                    progress?.Report(OperationProgress.CreateFromSteps(
                        i,
                        inputFiles.Count,
                        $"Processing file: {Path.GetFileName(file)}"));

                    try
                    {
                        var content = await File.ReadAllTextAsync(file, cancellationToken);
                        outputStream.Write(content);

                        // Add separator if specified and not the last file
                        if (!string.IsNullOrWhiteSpace(separator) && i < inputFiles.Count - 1)
                        {
                            outputStream.Write(separator);
                        }

                        totalSize += new FileInfo(file).Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to process file '{file}': {ex.Message}");
                    }
                }
            }

            progress?.Report(OperationProgress.Create(inputFiles.Count, inputFiles.Count, "File combination completed"));

            return FileCombineResult.SuccessResult(
                Path.GetFullPath(outputPath),
                inputFiles.Count,
                totalSize,
                DateTime.UtcNow - startTime);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File combination was cancelled");
            return FileCombineResult.FailureResult(
                "Operation was cancelled",
                duration: DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError($"File combination failed: {ex.Message}");
            return FileCombineResult.FailureResult(
                ex.Message,
                ex,
                DateTime.UtcNow - startTime);
        }
    }

    /// <summary>
    /// Validates that a file path is safe and doesn't contain path traversal attempts.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe, false otherwise.</returns>
    public bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            // Check for path traversal patterns
            var lowerPath = path.ToLowerInvariant();
            var pathTraversalPatterns = new[]
            {
                "..",
                "../",
                "..\\",
                "%2e%2e",
                "%2e%2e%2f",
                "%2e%2e%5c",
                "..%2f",
                "..%5c",
                "%252e%252e",
                "....//",
                "..\\..\\"
            };

            if (pathTraversalPatterns.Any(pattern => lowerPath.Contains(pattern)))
            {
                return false;
            }

            // Check for null bytes
            if (path.Contains('\0'))
            {
                return false;
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }

            // Try to get full path
            var fullPath = Path.GetFullPath(path);

            // Additional check: ensure the path doesn't resolve to sensitive system locations
            var sensitivePaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "/etc",
                "/usr/bin",
                "/usr/sbin",
                "/bin",
                "/sbin"
            };

            foreach (var sensitivePath in sensitivePaths)
            {
                if (!string.IsNullOrEmpty(sensitivePath) &&
                    fullPath.StartsWith(sensitivePath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The file size in bytes, or -1 if the file doesn't exist.</returns>
    public long GetFileSize(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path.</param>
    public void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to create directory: {ex.Message}");
        }
    }

    private List<string> GetInputFiles(FileCombineOptions options)
    {
        var input = options.Input;
        var files = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
        {
            return files;
        }

        // Check if input is a directory or file
        if (Directory.Exists(input))
        {
            var pattern = options.Pattern ?? "*.*";
            var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files.AddRange(Directory.GetFiles(input, pattern, searchOption));
        }
        else if (File.Exists(input))
        {
            files.Add(input);
        }
        else
        {
            // Try to treat as pattern
            var directory = Path.GetDirectoryName(input);
            var fileName = Path.GetFileName(input);

            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            if (Directory.Exists(directory))
            {
                var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                files.AddRange(Directory.GetFiles(directory, fileName, searchOption));
            }
        }

        // Sort files for consistent output
        files.Sort();

        return files;
    }
}
