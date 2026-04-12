namespace FurLab.CLI.Commands;

/// <summary>
/// Represents a single row in the consolidated CSV output.
/// </summary>
internal sealed record CsvRow(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Status,
    int RowCount,
    string Error,
    List<string> ColumnNames,
    List<Dictionary<string, string>> Data);
