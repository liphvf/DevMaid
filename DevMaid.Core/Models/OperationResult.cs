using System;

namespace DevMaid.Core.Models;

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

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <param name="duration">Optional duration.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult SuccessResult(string? message = null, TimeSpan? duration = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message,
            Duration = duration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">Optional duration.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult FailureResult(string errorMessage, Exception? exception = null, TimeSpan? duration = null)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
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

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The result data.</param>
    /// <param name="message">Optional success message.</param>
    /// <param name="duration">Optional duration.</param>
    /// <returns>A successful operation result with data.</returns>
    public static OperationResult<T> SuccessResult(T data, string? message = null, TimeSpan? duration = null)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Duration = duration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">Optional duration.</param>
    /// <returns>A failed operation result.</returns>
    public static new OperationResult<T> FailureResult(string errorMessage, Exception? exception = null, TimeSpan? duration = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
