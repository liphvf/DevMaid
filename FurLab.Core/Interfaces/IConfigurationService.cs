using Microsoft.Extensions.Configuration;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Defines a service for managing application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    IConfiguration Configuration { get; }
}
