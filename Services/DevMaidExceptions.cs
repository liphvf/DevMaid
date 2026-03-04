using System;

namespace DevMaid.Services;

/// <summary>
/// Base exception for all DevMaid-specific errors.
/// </summary>
public class DevMaidException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public DevMaidErrorCode ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevMaidException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DevMaidException(DevMaidErrorCode errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Error codes for DevMaid exceptions.
/// </summary>
public enum DevMaidErrorCode
{
    /// <summary>
    /// Database connection failed.
    /// </summary>
    DatabaseConnectionFailed,

    /// <summary>
    /// Database not found.
    /// </summary>
    DatabaseNotFound,

    /// <summary>
    /// Invalid connection string.
    /// </summary>
    InvalidConnectionString,

    /// <summary>
    /// File not found.
    /// </summary>
    FileNotFound,

    /// <summary>
    /// Path traversal attempted.
    /// </summary>
    PathTraversalAttempted,

    /// <summary>
    /// Invalid input provided.
    /// </summary>
    InvalidInput,

    /// <summary>
    /// PostgreSQL binary not found.
    /// </summary>
    PostgresBinaryNotFound,

    /// <summary>
    /// Backup operation failed.
    /// </summary>
    BackupFailed,

    /// <summary>
    /// Restore operation failed.
    /// </summary>
    RestoreFailed,

    /// <summary>
    /// Query execution failed.
    /// </summary>
    QueryExecutionFailed,

    /// <summary>
    /// Invalid host address.
    /// </summary>
    InvalidHost,

    /// <summary>
    /// Invalid port number.
    /// </summary>
    InvalidPort,

    /// <summary>
    /// Invalid username.
    /// </summary>
    InvalidUsername,

    /// <summary>
    /// Invalid PostgreSQL identifier.
    /// </summary>
    InvalidPostgreSQLIdentifier,

    /// <summary>
    /// Configuration error.
    /// </summary>
    ConfigurationError,

    /// <summary>
    /// Process execution failed.
    /// </summary>
    ProcessExecutionFailed
}

/// <summary>
/// Exception thrown when a database connection fails.
/// </summary>
public class DatabaseConnectionException : DevMaidException
{
    public DatabaseConnectionException(string message, Exception? innerException = null)
        : base(DevMaidErrorCode.DatabaseConnectionFailed, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a file is not found.
/// </summary>
public class DevMaidFileNotFoundException : DevMaidException
{
    public string FilePath { get; }

    public DevMaidFileNotFoundException(string filePath, Exception? innerException = null)
        : base(DevMaidErrorCode.FileNotFound, $"File not found: {filePath}", innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Exception thrown when a path traversal attempt is detected.
/// </summary>
public class PathTraversalException : DevMaidException
{
    public string AttemptedPath { get; }

    public PathTraversalException(string attemptedPath)
        : base(DevMaidErrorCode.PathTraversalAttempted, $"Path traversal not allowed: {attemptedPath}")
    {
        AttemptedPath = attemptedPath;
    }
}

/// <summary>
/// Exception thrown when a PostgreSQL binary is not found.
/// </summary>
public class PostgresBinaryNotFoundException : DevMaidException
{
    public string BinaryName { get; }

    public PostgresBinaryNotFoundException(string binaryName)
        : base(DevMaidErrorCode.PostgresBinaryNotFound, $"{binaryName} not found. Please ensure PostgreSQL is installed and {binaryName} is in your PATH.")
    {
        BinaryName = binaryName;
    }
}

/// <summary>
/// Exception thrown when a backup operation fails.
/// </summary>
public class BackupFailedException : DevMaidException
{
    public BackupFailedException(string message, Exception? innerException = null)
        : base(DevMaidErrorCode.BackupFailed, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a restore operation fails.
/// </summary>
public class RestoreFailedException : DevMaidException
{
    public RestoreFailedException(string message, Exception? innerException = null)
        : base(DevMaidErrorCode.RestoreFailed, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a query execution fails.
/// </summary>
public class QueryExecutionFailedException : DevMaidException
{
    public QueryExecutionFailedException(string message, Exception? innerException = null)
        : base(DevMaidErrorCode.QueryExecutionFailed, message, innerException)
    {
    }
}
