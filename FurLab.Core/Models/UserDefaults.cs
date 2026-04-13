namespace FurLab.Core.Models;

/// <summary>
/// Default settings applied across servers unless overridden.
/// </summary>
public class UserDefaults
{
    /// <summary>
    /// Default output format (csv).
    /// </summary>
    public string OutputFormat { get; set; } = "csv";

    /// <summary>
    /// Default output directory for CSV files.
    /// </summary>
    public string OutputDirectory { get; set; } = "./results";

    /// <summary>
    /// Default behavior for auto-discovering databases.
    /// </summary>
    public bool FetchAllDatabases { get; set; }

    /// <summary>
    /// Whether to require confirmation for destructive queries.
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;

    /// <summary>
    /// Default maximum degree of parallelism.
    /// </summary>
    public int MaxParallelism { get; set; } = 4;
}
