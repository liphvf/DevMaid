using FurLab.Core.Interfaces;
using FurLab.Core.Logging;

using Microsoft.Extensions.Configuration;

namespace FurLab.Core.Services;

/// <summary>
/// Provides centralized configuration management for the FurLab application.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigurationService"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public class ConfigurationService(ILogger logger) : IConfigurationService
{
    private IConfigurationRoot? _configuration;
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Lock _lock = new();

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
}
