
namespace DevMaid.CLI.Services;

/// <summary>
/// Exception thrown when a PostgreSQL binary is not found.
/// </summary>
public class PostgresBinaryNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresBinaryNotFoundException"/> class.
    /// </summary>
    /// <param name="binaryName">The name of the binary that was not found.</param>
    public PostgresBinaryNotFoundException(string binaryName)
        : base($"PostgreSQL binary '{binaryName}' not found. Please ensure PostgreSQL is installed and the binaries are in your PATH.")
    {
    }
}

/// <summary>
/// Exception thrown when a database backup fails.
/// </summary>
public class BackupFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackupFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BackupFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BackupFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a database restore fails.
/// </summary>
public class RestoreFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RestoreFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RestoreFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a file is not found.
/// </summary>
public class DevMaidFileNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevMaidFileNotFoundException"/> class.
    /// </summary>
    /// <param name="filePath">The path to the file that was not found.</param>
    public DevMaidFileNotFoundException(string filePath)
        : base($"File not found: {filePath}")
    {
    }
}

/// <summary>
/// Exception thrown when a path traversal attempt is detected.
/// </summary>
public class PathTraversalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTraversalException"/> class.
    /// </summary>
    /// <param name="path">The path that contains the traversal attempt.</param>
    public PathTraversalException(string path)
        : base($"Path traversal detected: {path}")
    {
    }
}
