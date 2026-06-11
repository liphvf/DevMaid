namespace FurLab.Core.Models.Api;

/// <summary>
/// Request to analyze a SQL query before execution.
/// </summary>
public sealed record QueryAnalyzeRequest(
    string Sql,
    List<string>? Servers,
    bool AllDatabases = false,
    string? Exclude = null);

/// <summary>
/// Response from query analysis.
/// </summary>
public sealed record QueryAnalyzeResponse(
    string QueryType,
    bool IsDestructive,
    int AffectedServers,
    int EstimatedDatabases,
    bool RequiresConfirmation);

/// <summary>
/// Request to execute a SQL query.
/// </summary>
public sealed record QueryExecuteRequest(
    string Sql,
    List<string>? Servers,
    bool AllDatabases = false,
    string? Exclude = null,
    bool Confirmed = false);

/// <summary>
/// Response after initiating query execution.
/// </summary>
public sealed record QueryExecuteResponse(
    string ExecutionId,
    string Status,
    string OutputDirectory);

/// <summary>
/// Response for query execution status.
/// </summary>
public sealed record QueryStatusResponse(
    string ExecutionId,
    string Status,
    int TotalDatabases,
    int Completed,
    int Failed,
    int InProgress,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string OutputDirectory,
    List<DatabaseResultResponse> Results);

/// <summary>
/// Simplified database result for status responses.
/// </summary>
public sealed record DatabaseResultResponse(
    string Server,
    string Database,
    string Status,
    int RowCount,
    double DurationMs,
    string? Error);

/// <summary>
/// Response for cancel request.
/// </summary>
public sealed record QueryCancelResponse(
    string ExecutionId,
    string Status);
