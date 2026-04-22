using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

using FurLab.Core.Interfaces;
using FurLab.Core.Logging;
using FurLab.Core.Models;

namespace FurLab.Core.Services;

/// <summary>
/// Service for checking application updates from GitHub releases.
/// </summary>
public partial class UpdateCheckService : IUpdateCheckService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _cacheFolder;
    private readonly string _cacheFilePath;

    private const string GitHubApiUrl = "https://api.github.com/repos/liphvf/FurLab/releases/latest";
    private const string GitHubReleasesUrl = "https://github.com/liphvf/FurLab/releases";
    private const int WingetTimeoutMs = 30000; // 30 seconds

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCheckService"/> class.
    /// </summary>
    public UpdateCheckService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _cacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FurLab");
        _cacheFilePath = Path.Combine(_cacheFolder, "update-cache.json");
    }

    /// <inheritdoc/>
    public async Task<string> DetectInstallationMethodAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if installed via winget
            var isWinget = await IsInstalledViaWingetAsync(cancellationToken);
            if (isWinget)
            {
                _logger.LogDebug("Installation method detected: winget");
                return "winget";
            }

            // Check if installed as dotnet tool
            var isDotnetTool = IsInstalledAsDotnetTool();
            if (isDotnetTool)
            {
                _logger.LogDebug("Installation method detected: dotnet-tool");
                return "dotnet-tool";
            }

            _logger.LogDebug("Installation method detected: manual");
            return "manual";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to detect installation method: {ex.Message}");
            return "manual";
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetLatestVersionFromGitHubAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FurLab-UpdateChecker");

            var response = await _httpClient.GetAsync(GitHubApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(tagName))
            {
                _logger.LogWarning("GitHub API returned empty tag_name");
                return null;
            }

            // Remove 'v' prefix if present
            var version = tagName.StartsWith('v') ? tagName[1..] : tagName;
            _logger.LogDebug($"Latest version from GitHub: {version}");
            return version;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP error checking GitHub releases: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("GitHub API request timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error checking GitHub releases: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<UpdateCache?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // GetCurrentVersion will automatically find the CLI assembly
            // (handles both dotnet run and single-file publish scenarios)
            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestVersionFromGitHubAsync(cancellationToken);

            if (latestVersion == null)
            {
                _logger.LogWarning("Could not retrieve latest version from GitHub");
                return null;
            }

            var installationMethod = await DetectInstallationMethodAsync(cancellationToken);
            var updateAvailable = IsNewerVersion(currentVersion, latestVersion);

            var cache = new UpdateCache
            {
                CheckedAt = DateTime.UtcNow,
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                UpdateAvailable = updateAvailable,
                ReleaseUrl = $"{GitHubReleasesUrl}/v{latestVersion}",
                InstallationMethod = installationMethod
            };

            SaveUpdateCache(cache);
            _logger.LogDebug($"Update check complete. Update available: {updateAvailable}");

            return cache;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during update check: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public UpdateCache? LoadUpdateCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(_cacheFilePath);
            var cache = JsonSerializer.Deserialize<UpdateCache>(json, GetSerializerOptions());
            return cache;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load update cache: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public void SaveUpdateCache(UpdateCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        try
        {
            Directory.CreateDirectory(_cacheFolder);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(cache, options);
            File.WriteAllText(_cacheFilePath, json);

            _logger.LogDebug($"Update cache saved to {_cacheFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save update cache: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public string GetCacheFilePath() => _cacheFilePath;

    /// <inheritdoc/>
    public string GetCurrentVersion(System.Reflection.Assembly? assembly = null)
    {
        // Try to find the CLI assembly in order of preference
        var targetAssembly = assembly
            ?? System.Reflection.Assembly.GetEntryAssembly()
            ?? FindCliAssembly()
            ?? System.Reflection.Assembly.GetExecutingAssembly();

        // Try AssemblyInformationalVersion first (Nerdbank.GitVersioning sets this)
        var informationalVersion = targetAssembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        if (!string.IsNullOrEmpty(informationalVersion))
        {
            // Extract semver from informational version (e.g., "1.1.41+abc123" -> "1.1.41")
            var match = InformationalVersionRegex().Match(informationalVersion);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        // Fallback to assembly version
        var version = targetAssembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0"; // Major.Minor.Build
    }

    /// <summary>
    /// Finds the FurLab.CLI assembly in loaded assemblies.
    /// Useful when GetEntryAssembly() returns null (e.g., single-file publish).
    /// </summary>
    private static System.Reflection.Assembly? FindCliAssembly()
    {
        try
        {
            // Search in all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // First try: look for assembly with name containing "FurLab.CLI"
            var cliAssembly = assemblies.FirstOrDefault(a =>
                a.GetName().Name?.Contains("FurLab.CLI", StringComparison.OrdinalIgnoreCase) == true);

            if (cliAssembly != null)
            {
                return cliAssembly;
            }

            // Second try: look for any assembly with GitVersioning attributes
            return assemblies.FirstOrDefault(a =>
            {
                var infoVersion = a.GetCustomAttributes(
                    typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();
                return infoVersion != null && InformationalVersionRegex().IsMatch(infoVersion.InformationalVersion ?? "");
            });
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public bool IsNewerVersion(string current, string latest)
    {
        try
        {
            var currentVersion = ParseVersion(current);
            var latestVersion = ParseVersion(latest);

            return latestVersion > currentVersion;
        }
        catch
        {
            // If parsing fails, do string comparison as fallback
            return !string.Equals(current, latest, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<bool> IsInstalledViaWingetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(WingetTimeoutMs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "list --id FurLab.CLI --exact",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            // Check if FurLab.CLI appears in the output
            return output.Contains("FurLab.CLI", StringComparison.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Winget detection timed out after 30 seconds");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Winget detection failed: {ex.Message}");
            return false;
        }
    }

    private bool IsInstalledAsDotnetTool()
    {
        try
        {
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var dotnetToolsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dotnet", "tools");

            // Check if the executable is in the dotnet tools folder
            return executablePath.StartsWith(dotnetToolsPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Dotnet tool detection failed: {ex.Message}");
            return false;
        }
    }

    private static Version ParseVersion(string versionString)
    {
        // Handle versions like "1.1.41" or "v1.1.41"
        var cleaned = versionString.Trim().TrimStart('v', 'V');

        // Try to extract version using regex for semver-like strings
        var match = VersionRegex().Match(cleaned);
        if (match.Success)
        {
            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            return new Version(major, minor, build);
        }

        // Fallback to standard Version parsing
        return Version.Parse(cleaned);
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    [GeneratedRegex(@"^(\d+)\.(\d+)(?:\.(\d+))?")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"^(\d+\.\d+(?:\.\d+)?)")]
    private static partial Regex InformationalVersionRegex();
}
