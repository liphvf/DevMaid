namespace DevMaid.CLI.CommandOptions;

/// <summary>
/// Options for database backup and restore commands.
/// </summary>
public class DatabaseCommandOptions
{
    /// <summary>Gets or sets the name of the database.</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether to operate on all databases.</summary>
    public bool All { get; set; }

    /// <summary>Gets or sets the database host address.</summary>
    public string? Host { get; set; }

    /// <summary>Gets or sets the database port.</summary>
    public string? Port { get; set; }

    /// <summary>Gets or sets the database username.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the database password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the output file or directory path.</summary>
    public string? OutputPath { get; set; }

    /// <summary>Gets or sets the input file path for restore operations.</summary>
    public string? InputFile { get; set; }

    /// <summary>Gets or sets table name patterns whose data should be excluded from the backup.</summary>
    public string[]? ExcludeTableData { get; set; }
}
