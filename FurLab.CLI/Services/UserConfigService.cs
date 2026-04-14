using FurLab.Core.Interfaces;
using FurLab.Core.Models;

using Microsoft.Extensions.DependencyInjection;

namespace FurLab.CLI.Services;

/// <summary>
/// Provides centralized user configuration management for the FurLab CLI.
/// This is a compatibility wrapper around the Core UserConfigService.
/// </summary>
public static class UserConfigService
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the user configuration service.
    /// </summary>
    private static IUserConfigService GetUserConfigService()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");
        }

        return _serviceProvider.GetRequiredService<IUserConfigService>();
    }

    /// <summary>
    /// Loads the complete user configuration from furlab.jsonc.
    /// </summary>
    public static UserConfig LoadConfig() => GetUserConfigService().LoadConfig();

    /// <summary>
    /// Saves the complete user configuration to furlab.jsonc.
    /// </summary>
    public static void SaveConfig(UserConfig config) => GetUserConfigService().SaveConfig(config);

    /// <summary>
    /// Gets all configured servers.
    /// </summary>
    public static IReadOnlyList<ServerConfigEntry> GetServers() => GetUserConfigService().GetServers();

    /// <summary>
    /// Gets a server by name.
    /// </summary>
    public static ServerConfigEntry? GetServer(string name) => GetUserConfigService().GetServer(name);

    /// <summary>
    /// Adds or updates a server configuration.
    /// </summary>
    public static void AddOrUpdateServer(ServerConfigEntry server) => GetUserConfigService().AddOrUpdateServer(server);

    /// <summary>
    /// Removes a server by name.
    /// </summary>
    public static bool RemoveServer(string name) => GetUserConfigService().RemoveServer(name);

    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static UserDefaults GetDefaults() => GetUserConfigService().GetDefaults();

    /// <summary>
    /// Gets the path to the user config file.
    /// </summary>
    public static string GetConfigFilePath() => GetUserConfigService().GetConfigFilePath();

    /// <summary>
    /// Checks if the user config file exists.
    /// </summary>
    public static bool ConfigFileExists() => GetUserConfigService().ConfigFileExists();

    /// <summary>
    /// Tries to load legacy appsettings.json configuration during migration period.
    /// </summary>
    public static UserConfig? TryLoadLegacyConfig() => GetUserConfigService().TryLoadLegacyConfig();

    /// <summary>
    /// Sets the encrypted password for a server identified by name.
    /// </summary>
    public static void SetEncryptedPassword(string serverName, string encryptedPassword) =>
        GetUserConfigService().SetEncryptedPassword(serverName, encryptedPassword);
}
