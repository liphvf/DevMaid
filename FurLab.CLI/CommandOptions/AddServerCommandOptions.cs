namespace FurLab.CLI.CommandOptions;

/// <summary>
/// Options for the settings db-servers add command.
/// </summary>
public class AddServerCommandOptions
{
    /// <summary>Server name (unique identifier).</summary>
    public string? Name { get; set; }

    /// <summary>Database host address.</summary>
    public string? Host { get; set; }

    /// <summary>Database port.</summary>
    public int Port { get; set; } = 5432;

    /// <summary>Database username.</summary>
    public string? Username { get; set; }

    /// <summary>Comma-separated list of specific databases.</summary>
    public string? Databases { get; set; }

    /// <summary>SSL mode.</summary>
    public string? SslMode { get; set; }

    /// <summary>Connection timeout in seconds.</summary>
    public int? Timeout { get; set; }

    /// <summary>Command timeout in seconds.</summary>
    public int? CommandTimeout { get; set; }

    /// <summary>Max degree of parallelism.</summary>
    public int? MaxParallelism { get; set; }

    /// <summary>Auto-discover all databases on server.</summary>
    public bool FetchAllDatabases { get; set; }

    /// <summary>Comma-separated patterns to exclude from auto-discovery.</summary>
    public string? ExcludePatterns { get; set; }

    /// <summary>Use interactive mode.</summary>
    public bool Interactive { get; set; }
}
