namespace FurLab.Core.Models;

/// <summary>
/// Configuration for automatic update checking.
/// </summary>
public class UpdateCheckConfig
{
    /// <summary>
    /// Whether automatic update checking is enabled.
    /// Defaults to true for winget, false for manual installations.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The installation method detected: "winget", "dotnet-tool", "manual", or null if not detected.
    /// </summary>
    public string? InstallationMethod { get; set; }

    /// <summary>
    /// When the installation method was last verified.
    /// </summary>
    public DateTime? MethodVerifiedAt { get; set; }

    /// <summary>
    /// When the next update check is due.
    /// </summary>
    public DateTime NextCheckDue { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether a check is currently in progress (prevents duplicate checks).
    /// </summary>
    public bool CheckInProgress { get; set; } = false;
}
