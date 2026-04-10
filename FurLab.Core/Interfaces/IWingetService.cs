
using FurLab.Core.Models;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Defines a service for Winget package operations.
/// </summary>
public interface IWingetService
{
    /// <summary>
    /// Creates a backup of installed Winget packages.
    /// </summary>
    /// <param name="options">The backup options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the backup operation with the result.</returns>
    Task<WingetBackupResult> BackupPackagesAsync(
        WingetBackupOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores Winget packages from a backup file.
    /// </summary>
    /// <param name="options">The restore options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the restore operation with the result.</returns>
    Task<WingetRestoreResult> RestorePackagesAsync(
        WingetRestoreOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all installed Winget packages.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a list of package IDs.</returns>
    Task<List<string>> ListPackagesAsync(CancellationToken cancellationToken = default);
}
