using System.Diagnostics;

using FurLab.Core.Interfaces;
using FurLab.Core.Logging;

namespace FurLab.Core.Services;

/// <summary>
/// Runs background tasks for update checking and installation method detection.
/// </summary>
public class BackgroundTaskRunner
{
    private readonly ILogger _logger;
    private readonly IUserConfigService _configService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly string _configFolder;
    private readonly string _lockFilePath;

    private const int LockFileTimeoutMinutes = 30;
    private const int WingetTimeoutMs = 30000; // 30 seconds

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundTaskRunner"/> class.
    /// </summary>
    public BackgroundTaskRunner(
        ILogger logger,
        IUserConfigService configService,
        IUpdateCheckService updateCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _updateCheckService = updateCheckService ?? throw new ArgumentNullException(nameof(updateCheckService));

        _configFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FurLab");
        _lockFilePath = Path.Combine(_configFolder, "update-check.lock");
    }

    /// <summary>
    /// Runs the detect installation method task asynchronously.
    /// </summary>
    public async Task RunDetectInstallMethodTaskAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting background task: detect-install-method");

            if (!TryAcquireLock())
            {
                _logger.LogDebug("Lock file exists, skipping detect-install-method task");
                return;
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(WingetTimeoutMs);

                var method = await _updateCheckService.DetectInstallationMethodAsync(cts.Token);
                var verifiedAt = DateTime.UtcNow;

                _configService.SetInstallationMethod(method, verifiedAt);
                _logger.LogDebug($"Installation method detected and saved: {method}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Detect installation method task timed out after 30 seconds");

                // Set manual as fallback and disable updates
                _configService.SetInstallationMethod("manual", DateTime.UtcNow);

                // Ensure it's disabled since we couldn't detect
                var config = _configService.GetUpdateCheckConfig();
                config.Enabled = false;
                _configService.SaveUpdateCheckConfig(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in detect-install-method task: {ex.Message}");
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>
    /// Runs the check update task asynchronously.
    /// </summary>
    public async Task RunCheckUpdateTaskAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting background task: check-update");

            if (!TryAcquireLock())
            {
                _logger.LogDebug("Lock file exists, skipping check-update task");
                return;
            }

            try
            {
                // Mark check as in progress
                var config = _configService.GetUpdateCheckConfig();
                config.CheckInProgress = true;
                _configService.SaveUpdateCheckConfig(config);

                // Perform the check
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(WingetTimeoutMs);

                var cache = await _updateCheckService.CheckForUpdateAsync(cts.Token);

                if (cache != null)
                {
                    _logger.LogDebug($"Update check completed. Available: {cache.UpdateAvailable}");
                }
                else
                {
                    _logger.LogWarning("Update check failed or returned no results");
                }

                // Schedule next check for 24 hours from now
                config.NextCheckDue = DateTime.UtcNow.AddHours(24);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Check update task timed out after 30 seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update check: {ex.Message}");
            }
            finally
            {
                // Mark check as complete
                var config = _configService.GetUpdateCheckConfig();
                config.CheckInProgress = false;
                config.NextCheckDue = DateTime.UtcNow.AddHours(24); // Retry in 24 hours even on failure
                _configService.SaveUpdateCheckConfig(config);

                ReleaseLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in check-update task: {ex.Message}");
            ReleaseLock();
        }
    }

    /// <summary>
    /// Checks if a background task should be spawned based on configuration.
    /// </summary>
    public bool ShouldSpawnBackgroundCheck()
    {
        var config = _configService.GetUpdateCheckConfig();

        // Check if enabled
        if (!config.Enabled)
        {
            return false;
        }

        // Check if it's time for next check
        if (DateTime.UtcNow < config.NextCheckDue)
        {
            return false;
        }

        // Check if already in progress
        if (config.CheckInProgress)
        {
            // If check has been in progress for too long, assume it died
            if (!IsLockFileValid())
            {
                _logger.LogDebug("Check marked in progress but lock expired, will spawn new check");
                return true;
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Spawns a background process to run the specified task.
    /// </summary>
    public void SpawnBackgroundTask(string taskName)
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName ?? "fur";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = $"--background-task {taskName}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            _logger.LogDebug($"Spawned background process for task: {taskName} (PID: {process.Id})");

            // Mark as in progress immediately
            var config = _configService.GetUpdateCheckConfig();
            config.CheckInProgress = true;
            _configService.SaveUpdateCheckConfig(config);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to spawn background process: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the installation method needs reverification (older than 30 days).
    /// </summary>
    public bool ShouldReverifyInstallationMethod()
    {
        var config = _configService.GetUpdateCheckConfig();

        // Never verified, needs detection
        if (string.IsNullOrEmpty(config.InstallationMethod))
        {
            return true;
        }

        // Verified more than 30 days ago
        if (config.MethodVerifiedAt.HasValue)
        {
            var daysSinceVerification = (DateTime.UtcNow - config.MethodVerifiedAt.Value).TotalDays;
            return daysSinceVerification > 30;
        }

        return true;
    }

    private bool TryAcquireLock()
    {
        try
        {
            // Check if lock file exists and is recent
            if (IsLockFileValid())
            {
                return false;
            }

            // Create or overwrite lock file
            Directory.CreateDirectory(_configFolder);
            File.WriteAllText(_lockFilePath, DateTime.UtcNow.ToString("O"));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to acquire lock: {ex.Message}");
            return false;
        }
    }

    private void ReleaseLock()
    {
        try
        {
            if (File.Exists(_lockFilePath))
            {
                File.Delete(_lockFilePath);
                _logger.LogDebug("Lock file released");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to release lock: {ex.Message}");
        }
    }

    private bool IsLockFileValid()
    {
        try
        {
            if (!File.Exists(_lockFilePath))
            {
                return false;
            }

            var lockContent = File.ReadAllText(_lockFilePath);
            if (DateTime.TryParse(lockContent, out var lockTime))
            {
                var age = DateTime.UtcNow - lockTime;
                if (age.TotalMinutes > LockFileTimeoutMinutes)
                {
                    // Lock is stale, delete it
                    _logger.LogDebug("Lock file is stale (older than 30 minutes), removing");
                    File.Delete(_lockFilePath);
                    return false;
                }
                return true;
            }

            // Invalid lock file content, delete it
            File.Delete(_lockFilePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error checking lock file: {ex.Message}");
            return false;
        }
    }
}
