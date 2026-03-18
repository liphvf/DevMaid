namespace DevMaid.Core.Models;

/// <summary>
/// Represents options for a file combine operation.
/// </summary>
public record FileCombineOptions
{
    /// <summary>
    /// Gets or sets the input file or directory path.
    /// </summary>
    public string Input { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the output file path.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the file pattern to match (e.g., "*.txt").
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets or sets whether to include subdirectories.
    /// </summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Gets or sets the separator to use between files.
    /// </summary>
    public string? Separator { get; init; }
}

/// <summary>
/// Represents options for a file split operation.
/// </summary>
public record FileSplitOptions
{
    /// <summary>
    /// Gets or sets the input file path.
    /// </summary>
    public string Input { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory path.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the number of lines per split file.
    /// </summary>
    public int LinesPerFile { get; init; }

    /// <summary>
    /// Gets or sets the prefix for split files.
    /// </summary>
    public string? Prefix { get; init; }
}
