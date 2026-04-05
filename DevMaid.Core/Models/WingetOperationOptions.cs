namespace DevMaid.Core.Models;

/// <summary>
/// Represents options for a Winget backup operation.
/// </summary>
public record WingetBackupOptions
{
    /// <summary>
    /// Gets or sets the output path for the backup file.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets or sets whether to include source information.
    /// </summary>
    public bool IncludeSource { get; init; }

    /// <summary>
    /// Gets or sets whether to include version information.
    /// </summary>
    public bool IncludeVersion { get; init; }
}

/// <summary>
/// Represents options for a Winget restore operation.
/// </summary>
public record WingetRestoreOptions
{
    /// <summary>
    /// Gets or sets the path to the backup file to restore.
    /// </summary>
    public string InputFile { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to ignore errors and continue with other packages.
    /// </summary>
    public bool IgnoreErrors { get; init; }

    /// <summary>
    /// Gets or sets whether to run the restore in interactive mode.
    /// </summary>
    public bool Interactive { get; init; }

    /// <summary>
    /// Gets or sets whether to skip dependencies.
    /// </summary>
    public bool SkipDependencies { get; init; }
}
