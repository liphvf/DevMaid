
using DevMaid.Core.Models;

namespace DevMaid.Core.Interfaces;

/// <summary>
/// Defines a service for file operations.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Combines multiple files into a single output file.
    /// </summary>
    /// <param name="options">The combine options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the combine operation with the result.</returns>
    Task<FileCombineResult> CombineFilesAsync(
        FileCombineOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a file path is safe and doesn't contain path traversal attempts.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe, false otherwise.</returns>
    bool IsValidPath(string path);

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The file size in bytes, or -1 if the file doesn't exist.</returns>
    long GetFileSize(string path);

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path.</param>
    void EnsureDirectoryExists(string path);
}
