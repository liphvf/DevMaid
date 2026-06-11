using System.Collections.Concurrent;
using System.Threading.Channels;

namespace FurLab.Api.Services;

/// <summary>
/// Manages Server-Sent Events (SSE) channels per execution, allowing multiple
/// consumers to receive real-time progress updates for the same execution.
/// </summary>
public class SseBroadcasterService
{
    private readonly ConcurrentDictionary<Guid, Channel<SseEvent>> _channels = new();
    private readonly ILogger<SseBroadcasterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SseBroadcasterService"/> class.
    /// </summary>
    public SseBroadcasterService(ILogger<SseBroadcasterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new SSE channel for the specified execution.
    /// </summary>
    public Channel<SseEvent> CreateChannel(Guid executionId)
    {
        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });
        _channels[executionId] = channel;
        _logger.LogDebug("Created SSE channel for execution {ExecutionId}", executionId);
        return channel;
    }

    /// <summary>
    /// Gets an existing SSE channel for the specified execution.
    /// </summary>
    public Channel<SseEvent>? GetChannel(Guid executionId)
    {
        _channels.TryGetValue(executionId, out var channel);
        return channel;
    }

    /// <summary>
    /// Broadcasts an event to all readers of the specified execution's channel.
    /// </summary>
    public async Task BroadcastAsync(Guid executionId, SseEvent @event, CancellationToken cancellationToken = default)
    {
        if (_channels.TryGetValue(executionId, out var channel))
        {
            await channel.Writer.WriteAsync(@event, cancellationToken);
        }
    }

    /// <summary>
    /// Completes the channel for the specified execution, signaling no more events.
    /// </summary>
    public void CompleteChannel(Guid executionId)
    {
        if (_channels.TryGetValue(executionId, out var channel))
        {
            channel.Writer.Complete();
            _logger.LogDebug("Completed SSE channel for execution {ExecutionId}", executionId);
        }
    }

    /// <summary>
    /// Removes the channel for the specified execution.
    /// </summary>
    public void RemoveChannel(Guid executionId)
    {
        _channels.TryRemove(executionId, out _);
        _logger.LogDebug("Removed SSE channel for execution {ExecutionId}", executionId);
    }
}

/// <summary>
/// Represents a single Server-Sent Event.
/// </summary>
public sealed record SseEvent(string EventType, string Data);
