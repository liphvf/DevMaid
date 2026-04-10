namespace FurLab.Core.Models;

/// <summary>
/// Represents the result of a database backup operation.
/// </summary>
public record DatabaseBackupResult : OperationResult
{
    /// <summary>
    /// Gets or sets the name of the database that was backed up.
    /// </summary>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// Gets or sets the path to the backup file.
    /// </summary>
    public string? BackupFilePath { get; init; }

    /// <summary>
    /// Gets or sets the size of the backup file in bytes.
    /// </summary>
    public long BackupFileSize { get; init; }

    /// <summary>
    /// Gets or sets the number of tables backed up.
    /// </summary>
    public int TablesBackedUp { get; init; }

    /// <summary>
    /// Creates a successful database backup result.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    /// <param name="backupFilePath">The backup file path.</param>
    /// <param name="backupFileSize">The backup file size in bytes.</param>
    /// <param name="tablesBackedUp">The number of tables backed up.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful database backup result.</returns>
    public static DatabaseBackupResult SuccessResult(
        string databaseName,
        string backupFilePath,
        long backupFileSize,
        int tablesBackedUp,
        TimeSpan duration)
    {
        return new DatabaseBackupResult
        {
            Success = true,
            DatabaseName = databaseName,
            BackupFilePath = backupFilePath,
            BackupFileSize = backupFileSize,
            TablesBackedUp = tablesBackedUp,
            Duration = duration,
            Message = $"Database '{databaseName}' backed up successfully to {backupFilePath}"
        };
    }

    /// <summary>
    /// Creates a failed database backup result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed database backup result.</returns>
    public static new DatabaseBackupResult FailureResult(
        string errorMessage,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        return new DatabaseBackupResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}

/// <summary>
/// Represents the result of a database restore operation.
/// </summary>
public record DatabaseRestoreResult : OperationResult
{
    /// <summary>
    /// Gets or sets the name of the database that was restored.
    /// </summary>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// Gets or sets the path to the backup file that was restored.
    /// </summary>
    public string? BackupFilePath { get; init; }

    /// <summary>
    /// Gets or sets the number of tables restored.
    /// </summary>
    public int TablesRestored { get; init; }

    /// <summary>
    /// Creates a successful database restore result.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    /// <param name="backupFilePath">The backup file path.</param>
    /// <param name="tablesRestored">The number of tables restored.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful database restore result.</returns>
    public static DatabaseRestoreResult SuccessResult(
        string databaseName,
        string backupFilePath,
        int tablesRestored,
        TimeSpan duration)
    {
        return new DatabaseRestoreResult
        {
            Success = true,
            DatabaseName = databaseName,
            BackupFilePath = backupFilePath,
            TablesRestored = tablesRestored,
            Duration = duration,
            Message = $"Database '{databaseName}' restored successfully from {backupFilePath}"
        };
    }

    /// <summary>
    /// Creates a failed database restore result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed database restore result.</returns>
    public static new DatabaseRestoreResult FailureResult(
        string errorMessage,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        return new DatabaseRestoreResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
