namespace FurLab.CLI.Commands;

/// <summary>
/// Represents a single query result produced by one server/database execution.
/// Carries the result data, execution metadata, and duration for progressive CSV writing and log entries.
/// </summary>
internal sealed record CsvRow(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Status,
    int RowCount,
    string Error,
    double DurationMs,
    List<string> ColumnNames,
    List<Dictionary<string, string>> Data);
