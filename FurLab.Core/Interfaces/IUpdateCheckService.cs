using FurLab.Core.Models;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Service for checking and managing application updates.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Detects the installation method by querying winget.
    /// </summary>
    /// <returns>"winget" if found via winget, "dotnet-tool" if in dotnet tools path, "manual" otherwise.</returns>
    Task<string> DetectInstallationMethodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest release version from GitHub API.
    /// </summary>
    /// <returns>The latest version string (e.g., "1.1.42") or null if failed.</returns>
    Task<string?> GetLatestVersionFromGitHubAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an update is available by comparing current and latest versions.
    /// </summary>
    /// <returns>UpdateCache with check results, or null if check failed.</returns>
    Task<UpdateCache?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the cached update check results.
    /// </summary>
    /// <returns>The cached results, or null if no cache exists.</returns>
    UpdateCache? LoadUpdateCache();

    /// <summary>
    /// Saves update check results to cache.
    /// </summary>
    void SaveUpdateCache(UpdateCache cache);

    /// <summary>
    /// Gets the path to the update cache file.
    /// </summary>
    string GetCacheFilePath();

    /// <summary>
    /// Gets the current application version from the specified assembly.
    /// If no assembly is specified, uses the entry assembly.
    /// </summary>
    string GetCurrentVersion(System.Reflection.Assembly? assembly = null);

    /// <summary>
    /// Compares two semantic version strings.
    /// </summary>
    /// <returns>True if latest is newer than current.</returns>
    bool IsNewerVersion(string current, string latest);
}
