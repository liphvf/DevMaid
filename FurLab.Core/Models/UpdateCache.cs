namespace FurLab.Core.Models;

/// <summary>
/// Cache of the last update check results.
/// Stored separately from user config in update-cache.json.
/// </summary>
public class UpdateCache
{
    /// <summary>
    /// When the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// The current installed version.
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// The latest available version from GitHub.
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether an update is available (Latest > Current).
    /// </summary>
    public bool UpdateAvailable { get; set; }

    /// <summary>
    /// URL to the release page.
    /// </summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The detected installation method when the check was performed.
    /// </summary>
    public string InstallationMethod { get; set; } = string.Empty;
}
