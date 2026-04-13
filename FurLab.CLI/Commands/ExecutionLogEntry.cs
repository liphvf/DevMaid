namespace FurLab.CLI.Commands;

internal sealed record ExecutionLogEntry(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Status,
    int RowCount,
    double DurationMs,
    string Error);
