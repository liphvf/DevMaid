namespace FurLab.CLI.Commands;

/// <summary>
/// Represents a single entry in the execution log CSV, written progressively after each query completes (success or failure).
/// </summary>
internal sealed record ExecutionLogEntry(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Status,
    int RowCount,
    double DurationMs,
    string Error);
