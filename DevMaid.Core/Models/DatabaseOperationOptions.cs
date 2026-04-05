namespace DevMaid.Core.Models;

/// <summary>
/// Represents options for a database backup operation.
/// </summary>
public record DatabaseBackupOptions
{
    /// <summary>
    /// Gets or sets the name of the database to backup.
    /// </summary>
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to backup all databases.
    /// </summary>
    public bool All { get; init; }

    /// <summary>
    /// Gets or sets the database host address.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string? Port { get; init; }

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets or sets the output path for the backup file.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets or sets the list of tables to exclude data from.
    /// </summary>
    public string[]? ExcludeTableData { get; init; }

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    public string? SslMode { get; init; }

    /// <summary>
    /// Gets or sets whether to create only the schema (no data).
    /// </summary>
    public bool SchemaOnly { get; init; }

    /// <summary>
    /// Gets or sets whether to include custom format options.
    /// </summary>
    public bool CustomFormat { get; init; }
}

/// <summary>
/// Represents options for a database restore operation.
/// </summary>
public record DatabaseRestoreOptions
{
    /// <summary>
    /// Gets or sets the name of the database to restore to.
    /// </summary>
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the backup file to restore.
    /// </summary>
    public string InputFile { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the database host address.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string? Port { get; init; }

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

    /// <summary>
    /// Gets or sets whether to clean the database before restoring.
    /// </summary>
    public bool Clean { get; init; }

    /// <summary>
    /// Gets or sets whether to create the database if it doesn't exist.
    /// </summary>
    public bool CreateDatabase { get; init; }
}

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
