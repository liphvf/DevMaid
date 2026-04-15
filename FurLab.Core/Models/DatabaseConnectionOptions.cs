namespace FurLab.Core.Models;

/// <summary>
/// Represents options for a database connection.
/// </summary>
public record DatabaseConnectionOptions
{
    /// <summary>
    /// Gets or sets the database host address.
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string Port { get; init; } = "5432";

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    public string? SslMode { get; init; }
}
