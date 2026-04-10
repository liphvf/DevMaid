using FurLab.Core.Models;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Defines a service for database operations.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Creates a backup of a PostgreSQL database.
    /// </summary>
    /// <param name="options">The backup options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the backup operation with the result.</returns>
    Task<DatabaseBackupResult> BackupAsync(
        DatabaseBackupOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a PostgreSQL database from a backup file.
    /// </summary>
    /// <param name="options">The restore options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the restore operation with the result.</returns>
    Task<DatabaseRestoreResult> RestoreAsync(
        DatabaseRestoreOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all databases on the PostgreSQL server.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a list of database names.</returns>
    Task<List<string>> ListDatabasesAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to a PostgreSQL database.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a boolean indicating success.</returns>
    Task<bool> TestConnectionAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken = default);
}
