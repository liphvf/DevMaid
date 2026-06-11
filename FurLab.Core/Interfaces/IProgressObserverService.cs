using FurLab.Core.Models;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Observes and reports progress during query execution across multiple servers and databases.
/// Implementations handle presentation (console, SSE, logging, etc.).
/// </summary>
public interface IProgressObserverService
{
    /// <summary>
    /// Called when query execution starts.
    /// </summary>
    Task OnStartedAsync(QueryExecutionInfo info, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a specific database query begins executing.
    /// </summary>
    Task OnExecutingAsync(DatabaseExecutionInfo info, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a specific database query completes successfully.
    /// </summary>
    Task OnCompletedAsync(DatabaseResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a specific database query fails.
    /// </summary>
    Task OnFailedAsync(DatabaseError error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when all database queries have finished (success or failure).
    /// </summary>
    Task OnFinishedAsync(ExecutionSummary summary, CancellationToken cancellationToken = default);
}
