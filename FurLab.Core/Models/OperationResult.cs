namespace FurLab.Core.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public record OperationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets a success message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets or sets an error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the exception if the operation failed due to an exception.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets the duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Represents the result of an operation with a specific return type.
/// </summary>
/// <typeparam name="T">The type of the result data.</typeparam>
public record OperationResult<T> : OperationResult
{
    /// <summary>
    /// Gets or sets the result data.
    /// </summary>
    public T? Data { get; init; }
}
