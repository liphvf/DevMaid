namespace FurLab.Core.Models;

/// <summary>
/// Information broadcast when query execution starts.
/// </summary>
public sealed record QueryExecutionInfo(
    string ExecutionId,
    int Servers,
    int Databases,
    DateTime Timestamp);

/// <summary>
/// Information about a specific database query beginning execution.
/// </summary>
public sealed record DatabaseExecutionInfo(
    string Server,
    string Database,
    DateTime Timestamp);

/// <summary>
/// Result of a successful database query execution.
/// </summary>
public sealed record DatabaseResult(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Status,
    int RowCount,
    string Error,
    double DurationMs,
    List<string> ColumnNames,
    List<Dictionary<string, string>> Data);

/// <summary>
/// Error information for a failed database query execution.
/// </summary>
public sealed record DatabaseError(
    string Server,
    string Database,
    DateTime ExecutedAt,
    string Message);

/// <summary>
/// Summary broadcast when all database queries have completed.
/// </summary>
public sealed record ExecutionSummary(
    int SuccessCount,
    int FailureCount,
    int TotalRows,
    string OutputDirectory);
