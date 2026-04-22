using FurLab.Core.Interfaces;
using FurLab.Core.Logging;

using Spectre.Console;

namespace FurLab.Core.Services;

/// <summary>
/// Handles automatic update checking and notification display.
/// Called at the start of CLI command execution.
/// </summary>
public class UpdateCheckNotifier
{
    private readonly ILogger _logger;
    private readonly IUserConfigService _configService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly BackgroundTaskRunner _backgroundRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCheckNotifier"/> class.
    /// </summary>
    public UpdateCheckNotifier(
        ILogger logger,
        IUserConfigService configService,
        IUpdateCheckService updateCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _updateCheckService = updateCheckService ?? throw new ArgumentNullException(nameof(updateCheckService));
        _backgroundRunner = new BackgroundTaskRunner(logger, configService, updateCheckService);
    }

    /// <summary>
    /// Checks for updates and displays notification if available.
    /// Should be called at the start of command execution.
    /// </summary>
    public void CheckAndNotify()
    {
        try
        {
            // Step 1: Check if we need to detect installation method (first run or 30+ days)
            if (_backgroundRunner.ShouldReverifyInstallationMethod())
            {
                _logger.LogDebug("Installation method needs detection or reverification");
                _backgroundRunner.SpawnBackgroundTask("detect-install-method");
            }
            // Step 2: Check if we need to check for updates
            else if (_backgroundRunner.ShouldSpawnBackgroundCheck())
            {
                _logger.LogDebug("Update check is due, spawning background task");
                _backgroundRunner.SpawnBackgroundTask("check-update");
            }

            // Step 3: Display notification if update is available
            DisplayUpdateNotificationIfAvailable();
        }
        catch (Exception ex)
        {
            // Never fail the main command due to update check issues
            _logger.LogDebug($"Update check notification failed: {ex.Message}");
        }
    }

    private void DisplayUpdateNotificationIfAvailable()
    {
        var cache = _updateCheckService.LoadUpdateCache();

        if (cache == null || !cache.UpdateAvailable)
        {
            return;
        }

        // Don't show if cache is older than 7 days (stale)
        var cacheAge = DateTime.UtcNow - cache.CheckedAt;
        if (cacheAge.TotalDays > 7)
        {
            _logger.LogDebug("Update cache is stale, not displaying notification");
            return;
        }

        var method = cache.InstallationMethod;
        var updateCommand = method switch
        {
            "winget" => "winget upgrade FurLab.CLI",
            "dotnet-tool" => "dotnet tool update -g FurLab",
            _ => null
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(
                new Markup($"""
                    [yellow]📦 Nova versão disponível![/]

                    Instalada: [grey]{cache.CurrentVersion}[/]
                    Disponível: [green]{cache.LatestVersion}[/]
                    """ +
                    (updateCommand != null ? $"\n\nPara atualizar:\n  [blue]{updateCommand}[/]" : $"\n\nBaixe em:\n  [blue]{cache.ReleaseUrl}[/]")
                )
            )
            {
                Header = new PanelHeader("[yellow]Atualização Disponível[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            }
        );
        AnsiConsole.WriteLine();
    }
}
