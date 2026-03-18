using System.Collections.Generic;

namespace DevMaid.Core.Models;

/// <summary>
/// Represents the result of a Winget backup operation.
/// </summary>
public record WingetBackupResult : OperationResult
{
    /// <summary>
    /// Gets or sets the path to the backup file.
    /// </summary>
    public string? BackupFilePath { get; init; }

    /// <summary>
    /// Gets or sets the number of packages backed up.
    /// </summary>
    public int PackagesBackedUp { get; init; }

    /// <summary>
    /// Gets or sets the list of package IDs that were backed up.
    /// </summary>
    public List<string>? PackageIds { get; init; }

    /// <summary>
    /// Creates a successful Winget backup result.
    /// </summary>
    /// <param name="backupFilePath">The backup file path.</param>
    /// <param name="packagesBackedUp">The number of packages backed up.</param>
    /// <param name="packageIds">The list of package IDs.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful Winget backup result.</returns>
    public static WingetBackupResult SuccessResult(
        string backupFilePath,
        int packagesBackedUp,
        List<string>? packageIds,
        TimeSpan duration)
    {
        return new WingetBackupResult
        {
            Success = true,
            BackupFilePath = backupFilePath,
            PackagesBackedUp = packagesBackedUp,
            PackageIds = packageIds,
            Duration = duration,
            Message = $"Successfully backed up {packagesBackedUp} packages to {backupFilePath}"
        };
    }

    /// <summary>
    /// Creates a failed Winget backup result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed Winget backup result.</returns>
    public static new WingetBackupResult FailureResult(
        string errorMessage,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        return new WingetBackupResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}

/// <summary>
/// Represents the result of a Winget restore operation.
/// </summary>
public record WingetRestoreResult : OperationResult
{
    /// <summary>
    /// Gets or sets the path to the backup file that was restored.
    /// </summary>
    public string? BackupFilePath { get; init; }

    /// <summary>
    /// Gets or sets the number of packages successfully restored.
    /// </summary>
    public int PackagesRestored { get; init; }

    /// <summary>
    /// Gets or sets the number of packages that failed to restore.
    /// </summary>
    public int PackagesFailed { get; init; }

    /// <summary>
    /// Gets or sets the list of package IDs that failed to restore.
    /// </summary>
    public List<string>? FailedPackageIds { get; init; }

    /// <summary>
    /// Creates a successful Winget restore result.
    /// </summary>
    /// <param name="backupFilePath">The backup file path.</param>
    /// <param name="packagesRestored">The number of packages restored.</param>
    /// <param name="packagesFailed">The number of packages failed.</param>
    /// <param name="failedPackageIds">The list of failed package IDs.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful Winget restore result.</returns>
    public static WingetRestoreResult SuccessResult(
        string backupFilePath,
        int packagesRestored,
        int packagesFailed,
        List<string>? failedPackageIds,
        TimeSpan duration)
    {
        return new WingetRestoreResult
        {
            Success = true,
            BackupFilePath = backupFilePath,
            PackagesRestored = packagesRestored,
            PackagesFailed = packagesFailed,
            FailedPackageIds = failedPackageIds,
            Duration = duration,
            Message = $"Restored {packagesRestored} packages (failed: {packagesFailed}) from {backupFilePath}"
        };
    }

    /// <summary>
    /// Creates a failed Winget restore result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed Winget restore result.</returns>
    public static new WingetRestoreResult FailureResult(
        string errorMessage,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        return new WingetRestoreResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
