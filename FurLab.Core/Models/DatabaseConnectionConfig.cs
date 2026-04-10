namespace FurLab.Core.Models;

/// <summary>
/// Represents database connection configuration.
/// </summary>
public record DatabaseConnectionConfig
{
    /// <summary>
    /// Gets or sets the database host address.
    /// </summary>
    public string? Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string? Port { get; set; } = "5432";

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string? Password { get; set; }
}
