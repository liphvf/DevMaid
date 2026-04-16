using System.Text.RegularExpressions;

using FurLab.Core.Interfaces;
using FurLab.Core.Logging;
using FurLab.Core.Models;

using Microsoft.Extensions.Configuration;

namespace FurLab.Core.Services;

/// <summary>
/// Provides centralized configuration management for the FurLab application.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigurationService"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class ConfigurationService(ILogger logger) : IConfigurationService
{
    private IConfigurationRoot? _configuration;
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Lock _lock = new();
    private static readonly Regex ValidHostnameRegex = ValidHostnameRegexGenerated();

    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    _configuration ??= BuildConfiguration();
                }
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Gets the database connection configuration.
    /// </summary>
    /// <returns>The database connection configuration.</returns>
    public DatabaseConnectionConfig GetDatabaseConfig()
    {
        var dbConfig = Configuration.GetSection("Database").Get<DatabaseConnectionConfig>() ?? new DatabaseConnectionConfig();

        ValidateDatabaseConfig(dbConfig);

        return dbConfig;
    }

    /// <summary>
    /// Validates database configuration values.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void ValidateDatabaseConfig(DatabaseConnectionConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Host) && !IsValidHost(config.Host))
        {
            _logger.LogWarning("Invalid database host format: {Host}. Using default.", config.Host);
        }

        if (!string.IsNullOrWhiteSpace(config.Port))
        {
            if (!int.TryParse(config.Port, out var port) || port < 1 || port > 65535)
            {
                _logger.LogWarning("Invalid database port: {Port}. Using default.", config.Port);
            }
        }

        if (!string.IsNullOrWhiteSpace(config.Username) && !IsValidUsername(config.Username))
        {
            _logger.LogWarning("Invalid database username format: {Username}.", config.Username);
        }
    }

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            _configuration = BuildConfiguration();
        }
        _logger.LogDebug("Configuration reloaded");
    }

    private static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (host == "localhost" || host == "127.0.0.1" || host == "::1")
        {
            return true;
        }

        return ValidHostnameRegex.IsMatch(host);
    }

    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return Regex.IsMatch(username, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    private IConfigurationRoot BuildConfiguration()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "FurLab");
        _ = Directory.CreateDirectory(configFolder);

        var configPath = Path.Combine(configFolder, "appsettings.json");

        _logger.LogDebug($"Building configuration from: {configPath}");

        return new ConfigurationBuilder()
            .SetBasePath(configFolder)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string GetConfigFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "FurLab");
        return Path.Combine(configFolder, "appsettings.json");
    }

    [GeneratedRegex(@"^(localhost|127\.\d+\.\d+\.\d+|::1|[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])*(\.[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])*)*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ValidHostnameRegexGenerated();
}