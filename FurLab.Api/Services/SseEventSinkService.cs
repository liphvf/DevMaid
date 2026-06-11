using System.Text.Json;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;

namespace FurLab.Api.Services;

/// <summary>
/// Implements <see cref="IProgressObserverService"/> by broadcasting events
/// via Server-Sent Events (SSE) for real-time frontend updates.
/// </summary>
public class SseEventSinkService : IProgressObserverService
{
    private readonly SseBroadcasterService _broadcaster;
    private readonly StatusTrackerService _statusTracker;
    private readonly Guid _executionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SseEventSinkService"/> class.
    /// </summary>
    public SseEventSinkService(Guid executionId, SseBroadcasterService broadcaster, StatusTrackerService statusTracker)
    {
        _executionId = executionId;
        _broadcaster = broadcaster;
        _statusTracker = statusTracker;
    }

    /// <inheritdoc/>
    public async Task OnStartedAsync(QueryExecutionInfo info, CancellationToken cancellationToken = default)
    {
        _statusTracker.Initialize(_executionId, info);
        await BroadcastAsync("execution-started", info, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task OnExecutingAsync(DatabaseExecutionInfo info, CancellationToken cancellationToken = default)
    {
        _statusTracker.MarkExecuting(_executionId, info);
        await BroadcastAsync("query-executing", info, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task OnCompletedAsync(DatabaseResult result, CancellationToken cancellationToken = default)
    {
        _statusTracker.MarkCompleted(_executionId, result);
        await BroadcastAsync("query-completed", result, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task OnFailedAsync(DatabaseError error, CancellationToken cancellationToken = default)
    {
        _statusTracker.MarkFailed(_executionId, error);
        await BroadcastAsync("query-failed", error, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task OnFinishedAsync(ExecutionSummary summary, CancellationToken cancellationToken = default)
    {
        _statusTracker.MarkFinished(_executionId, summary);
        await BroadcastAsync("execution-completed", summary, cancellationToken);
        _broadcaster.CompleteChannel(_executionId);
    }

    private async Task BroadcastAsync<T>(string eventType, T data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await _broadcaster.BroadcastAsync(_executionId, new SseEvent(eventType, json), cancellationToken);
    }
}
