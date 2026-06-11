using System.Collections.Concurrent;
using FurLab.Core.Models;

namespace FurLab.Api.Services;

/// <summary>
/// Tracks the status of query executions for the <c>GET /api/query/status</c> endpoint,
/// enabling client reconnection and polling.
/// </summary>
public class StatusTrackerService
{
    private readonly ConcurrentDictionary<Guid, ExecutionStatus> _statuses = new();

    /// <summary>
    /// Initializes tracking for a new execution.
    /// </summary>
    public void Initialize(Guid executionId, QueryExecutionInfo info)
    {
        _statuses[executionId] = new ExecutionStatus
        {
            ExecutionId = executionId,
            Status = "running",
            StartedAt = DateTime.UtcNow,
            TotalDatabases = info.Databases,
            Completed = 0,
            Failed = 0,
            InProgress = 0,
            Results = []
        };
    }

    /// <summary>
    /// Marks a database as currently executing.
    /// </summary>
    public void MarkExecuting(Guid executionId, DatabaseExecutionInfo info)
    {
        if (_statuses.TryGetValue(executionId, out var status))
        {
            status.InProgress++;
        }
    }

    /// <summary>
    /// Marks a database execution as completed successfully.
    /// </summary>
    public void MarkCompleted(Guid executionId, DatabaseResult result)
    {
        if (_statuses.TryGetValue(executionId, out var status))
        {
            status.InProgress--;
            status.Completed++;
            status.Results.Add(result);
            status.TotalRows += result.RowCount;
        }
    }

    /// <summary>
    /// Marks a database execution as failed.
    /// </summary>
    public void MarkFailed(Guid executionId, DatabaseError error)
    {
        if (_statuses.TryGetValue(executionId, out var status))
        {
            status.InProgress--;
            status.Failed++;
            status.Results.Add(new DatabaseResult(
                error.Server, error.Database, error.ExecutedAt, "Error", 0, error.Message, 0, [], []));
        }
    }

    /// <summary>
    /// Marks the entire execution as finished.
    /// </summary>
    public void MarkFinished(Guid executionId, ExecutionSummary summary)
    {
        if (_statuses.TryGetValue(executionId, out var status))
        {
            status.Status = "completed";
            status.CompletedAt = DateTime.UtcNow;
            status.OutputDirectory = summary.OutputDirectory;
        }
    }

    /// <summary>
    /// Gets the current status of an execution.
    /// </summary>
    public ExecutionStatus? GetStatus(Guid executionId)
    {
        _statuses.TryGetValue(executionId, out var status);
        return status;
    }

    /// <summary>
    /// Removes a completed execution from tracking.
    /// </summary>
    public bool Remove(Guid executionId) => _statuses.TryRemove(executionId, out _);
}

/// <summary>
/// Represents the current status of a query execution.
/// </summary>
public class ExecutionStatus
{
    /// <summary>
    /// The unique execution identifier.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// The execution status: running, completed, cancelled, or failed.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the execution completed, if applicable.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total number of databases to execute against.
    /// </summary>
    public int TotalDatabases { get; set; }

    /// <summary>
    /// Number of successfully completed database executions.
    /// </summary>
    public int Completed { get; set; }

    /// <summary>
    /// Number of failed database executions.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Number of database executions currently in progress.
    /// </summary>
    public int InProgress { get; set; }

    /// <summary>
    /// Total rows returned across all successful executions.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// The output directory containing CSV files.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Results for each database execution.
    /// </summary>
    public List<DatabaseResult> Results { get; set; } = [];
}
