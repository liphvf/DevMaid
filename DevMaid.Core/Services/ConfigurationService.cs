using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Models;

namespace DevMaid.Core.Services;

/// <summary>
/// Provides centralized configuration management for the DevMaid application.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private IConfigurationRoot? _configuration;
    private readonly ILogger _logger;
    private readonly object _lock = new object();
    private static readonly Regex ValidHostnameRegex = new Regex(
        @"^(localhost|127\.\d+\.\d+\.\d+|::1|[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])*(\.[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])*)*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConfigurationService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

    /// <summary>
    /// Updates the database connection configuration.
    /// </summary>
    /// <param name="config">The configuration to set.</param>
    public void UpdateDatabaseConfig(DatabaseConnectionConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var configPath = GetConfigFilePath();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "DevMaid");
        Directory.CreateDirectory(configFolder);

        var existingConfig = new Dictionary<string, string?>();
        if (File.Exists(configPath))
        {
            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    existingConfig[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        existingConfig["Database:Host"] = config.Host ?? "localhost";
        existingConfig["Database:Port"] = config.Port ?? "5432";
        existingConfig["Database:Username"] = config.Username;
        existingConfig["Database:Password"] = config.Password;

        var configLines = existingConfig
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{kvp.Key}={kvp.Value}")
            .ToArray();

        File.WriteAllLines(configPath, configLines);

        _logger.LogDebug("Database configuration updated");

        Reload();
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

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value, or null if not found.</returns>
    public string? GetValue(string key)
    {
        return Configuration[key];
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    public void SetValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        var configPath = GetConfigFilePath();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "DevMaid");
        Directory.CreateDirectory(configFolder);

        var existingConfig = new Dictionary<string, string?>();
        if (File.Exists(configPath))
        {
            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    existingConfig[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        existingConfig[key] = value;

        var configLines = existingConfig
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{kvp.Key}={kvp.Value}")
            .ToArray();

        File.WriteAllLines(configPath, configLines);

        _logger.LogDebug($"Configuration value '{key}' updated");

        Reload();
    }

    private IConfigurationRoot BuildConfiguration()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "DevMaid");
        Directory.CreateDirectory(configFolder);

        var configPath = Path.Combine(configFolder, "appsettings.json");

        _logger.LogDebug($"Building configuration from: {configPath}");

        return new ConfigurationBuilder()
            .SetBasePath(configFolder)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private string GetConfigFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "DevMaid");
        return Path.Combine(configFolder, "appsettings.json");
    }
}
