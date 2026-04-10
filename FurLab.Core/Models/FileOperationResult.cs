namespace FurLab.Core.Models;

/// <summary>
/// Represents the result of a file combine operation.
/// </summary>
public record FileCombineResult : OperationResult
{
    /// <summary>
    /// Gets or sets the path to the combined output file.
    /// </summary>
    public string? OutputFilePath { get; init; }

    /// <summary>
    /// Gets or sets the number of files that were combined.
    /// </summary>
    public int FilesCombined { get; init; }

    /// <summary>
    /// Gets or sets the total size of the combined file in bytes.
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// Creates a successful file combine result.
    /// </summary>
    /// <param name="outputFilePath">The output file path.</param>
    /// <param name="filesCombined">The number of files combined.</param>
    /// <param name="totalSize">The total size in bytes.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful file combine result.</returns>
    public static FileCombineResult SuccessResult(
        string outputFilePath,
        int filesCombined,
        long totalSize,
        TimeSpan duration)
    {
        return new FileCombineResult
        {
            Success = true,
            OutputFilePath = outputFilePath,
            FilesCombined = filesCombined,
            TotalSize = totalSize,
            Duration = duration,
            Message = $"Successfully combined {filesCombined} files to {outputFilePath}"
        };
    }

    /// <summary>
    /// Creates a failed file combine result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed file combine result.</returns>
    public static new FileCombineResult FailureResult(
        string errorMessage,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        return new FileCombineResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
