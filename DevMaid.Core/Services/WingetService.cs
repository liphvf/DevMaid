using System.Text.Json;

using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Models;

namespace DevMaid.Core.Services;

/// <summary>
/// Provides methods for Winget package operations including backup and restore.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WingetService"/> class.
/// </remarks>
/// <param name="processExecutor">The process executor instance.</param>
/// <param name="logger">The logger instance.</param>
public class WingetService(IProcessExecutor processExecutor, ILogger logger) : IWingetService
{
    private readonly IProcessExecutor _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Creates a backup of installed Winget packages.
    /// </summary>
    /// <param name="options">The backup options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the backup operation with the result.</returns>
    public async Task<WingetBackupResult> BackupPackagesAsync(
        WingetBackupOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startTime = DateTime.UtcNow;

        try
        {
            // List all installed packages
            progress?.Report(OperationProgress.Create(0, 2, "Listing installed packages..."));

            var packages = await ListPackagesAsync(cancellationToken);

            if (packages.Count == 0)
            {
                return WingetBackupResult.FailureResult(
                    "No packages found",
                    duration: DateTime.UtcNow - startTime);
            }

            progress?.Report(OperationProgress.Create(1, 2, $"Found {packages.Count} packages"));

            // Determine output path
            var outputPath = options.OutputPath;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = "winget-packages.json";
            }

            // Create backup data
            var backupData = new
            {
                CreatedAt = DateTime.UtcNow,
                PackageCount = packages.Count,
                Packages = packages.Select(id => new
                {
                    Id = id,
                    Source = options.IncludeSource ? GetPackageSource(id, cancellationToken).Result : (string?)null,
                    Version = options.IncludeVersion ? GetPackageVersion(id, cancellationToken).Result : (string?)null
                }).ToArray()
            };

            // Write to file
            progress?.Report(OperationProgress.Create(2, 2, "Writing backup file..."));

            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(outputPath, json, cancellationToken);

            var fileInfo = new FileInfo(outputPath);

            progress?.Report(OperationProgress.Create(2, 2, "Backup completed"));

            return WingetBackupResult.SuccessResult(
                Path.GetFullPath(outputPath),
                packages.Count,
                packages,
                DateTime.UtcNow - startTime);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Winget backup was cancelled");
            return WingetBackupResult.FailureResult(
                "Operation was cancelled",
                duration: DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Winget backup failed: {ex.Message}");
            return WingetBackupResult.FailureResult(
                ex.Message,
                ex,
                DateTime.UtcNow - startTime);
        }
    }

    /// <summary>
    /// Restores Winget packages from a backup file.
    /// </summary>
    /// <param name="options">The restore options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the restore operation with the result.</returns>
    public async Task<WingetRestoreResult> RestorePackagesAsync(
        WingetRestoreOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startTime = DateTime.UtcNow;

        try
        {
            // Read backup file
            if (!File.Exists(options.InputFile))
            {
                return WingetRestoreResult.FailureResult(
                    $"Backup file not found: {options.InputFile}",
                    duration: DateTime.UtcNow - startTime);
            }

            progress?.Report(OperationProgress.Create(0, 1, "Reading backup file..."));

            var json = await File.ReadAllTextAsync(options.InputFile, cancellationToken);
            var backupData = JsonSerializer.Deserialize<JsonElement>(json);

            if (backupData.ValueKind == JsonValueKind.Undefined || backupData.ValueKind == JsonValueKind.Null)
            {
                return WingetRestoreResult.FailureResult(
                    "Invalid backup file format",
                    duration: DateTime.UtcNow - startTime);
            }

            var packagesElement = backupData.GetProperty("Packages");
            var packages = new List<string>();

            foreach (var package in packagesElement.EnumerateArray())
            {
                packages.Add(package.GetProperty("Id").GetString() ?? string.Empty);
            }

            if (packages.Count == 0)
            {
                return WingetRestoreResult.FailureResult(
                    "No packages found in backup file",
                    duration: DateTime.UtcNow - startTime);
            }

            progress?.Report(OperationProgress.Create(0, packages.Count, $"Restoring {packages.Count} packages..."));

            // Restore packages
            var successCount = 0;
            var failureCount = 0;
            var failedPackageIds = new List<string>();

            for (var i = 0; i < packages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packageId = packages[i];
                progress?.Report(OperationProgress.CreateFromSteps(
                    i,
                    packages.Count,
                    $"Restoring package: {packageId}"));

                try
                {
                    var result = await InstallPackageAsync(packageId, options, cancellationToken);
                    if (result.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        failedPackageIds.Add(packageId);

                        if (!options.IgnoreErrors)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    failedPackageIds.Add(packageId);
                    _logger.LogWarning($"Failed to restore package '{packageId}': {ex.Message}");

                    if (!options.IgnoreErrors)
                    {
                        break;
                    }
                }
            }

            progress?.Report(OperationProgress.Create(packages.Count, packages.Count, "Restore completed"));

            return WingetRestoreResult.SuccessResult(
                Path.GetFullPath(options.InputFile),
                successCount,
                failureCount,
                failedPackageIds,
                DateTime.UtcNow - startTime);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Winget restore was cancelled");
            return WingetRestoreResult.FailureResult(
                "Operation was cancelled",
                duration: DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Winget restore failed: {ex.Message}");
            return WingetRestoreResult.FailureResult(
                ex.Message,
                ex,
                DateTime.UtcNow - startTime);
        }
    }

    /// <summary>
    /// Lists all installed Winget packages.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a list of package IDs.</returns>
    public async Task<List<string>> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processExecutor.ExecuteAsync(
                new ProcessExecutionOptions
                {
                    FileName = "winget",
                    Arguments = "list --accept-source-agreements --accept-package-agreements"
                },
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning($"Failed to list packages: {result.StandardError}");
                return [];
            }

            // Parse output to get package IDs
            var packages = new List<string>();
            var lines = result.StandardOutput.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Skip header lines and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) ||
                    trimmedLine.StartsWith("Name") ||
                    trimmedLine.StartsWith("---") ||
                    trimmedLine.Contains("No installed package found"))
                {
                    continue;
                }

                // Parse package ID (usually the second column)
                var parts = trimmedLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    packages.Add(parts[1]);
                }
            }

            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to list packages: {ex.Message}");
            return [];
        }
    }

    private async Task<ProcessExecutionResult> InstallPackageAsync(
        string packageId,
        WingetRestoreOptions options,
        CancellationToken cancellationToken)
    {
        var arguments = $"install --id \"{packageId}\" --accept-source-agreements --accept-package-agreements";

        if (!options.Interactive)
        {
            arguments += " --silent";
        }

        if (options.SkipDependencies)
        {
            arguments += " --ignore-dependencies";
        }

        return await _processExecutor.ExecuteAsync(
            new ProcessExecutionOptions
            {
                FileName = "winget",
                Arguments = arguments
            },
            cancellationToken: cancellationToken);
    }

    private async Task<string?> GetPackageSource(string packageId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processExecutor.ExecuteAsync(
                new ProcessExecutionOptions
                {
                    FileName = "winget",
                    Arguments = $"show --id \"{packageId}\" --accept-source-agreements"
                },
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                return null;
            }

            // Parse source from output
            var lines = result.StandardOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Source"))
                {
                    var parts = line.Split([':'], 2);
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetPackageVersion(string packageId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processExecutor.ExecuteAsync(
                new ProcessExecutionOptions
                {
                    FileName = "winget",
                    Arguments = $"show --id \"{packageId}\" --accept-source-agreements"
                },
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                return null;
            }

            // Parse version from output
            var lines = result.StandardOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Version"))
                {
                    var parts = line.Split([':'], 2);
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
