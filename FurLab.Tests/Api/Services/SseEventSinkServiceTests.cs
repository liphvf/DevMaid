using FurLab.Api.Services;
using FurLab.Core.Models;

using Microsoft.Extensions.Logging.Abstractions;

namespace FurLab.Tests.Api.Services;

[TestClass]
public class SseEventSinkServiceTests
{
    private SseBroadcasterService _broadcaster = null!;
    private StatusTrackerService _statusTracker = null!;
    private readonly Guid _executionId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _broadcaster = new SseBroadcasterService(NullLogger<SseBroadcasterService>.Instance);
        _statusTracker = new StatusTrackerService();
        _broadcaster.CreateChannel(_executionId);
    }

    [TestMethod(DisplayName = "OnStartedAsync_BroadcastsExecutionStartedEvent")]
    public async Task OnStartedAsync_BroadcastsExecutionStartedEvent()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);

        await sink.OnStartedAsync(new QueryExecutionInfo(_executionId.ToString(), 1, 3, DateTime.UtcNow));

        var channel = _broadcaster.GetChannel(_executionId);
        var evt = await channel!.Reader.ReadAsync();
        Assert.AreEqual("execution-started", evt.EventType);
    }

    [TestMethod(DisplayName = "OnExecutingAsync_BroadcastsQueryExecutingEvent")]
    public async Task OnExecutingAsync_BroadcastsQueryExecutingEvent()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);

        await sink.OnExecutingAsync(new DatabaseExecutionInfo("srv", "db", DateTime.UtcNow));

        var channel = _broadcaster.GetChannel(_executionId);
        var evt = await channel!.Reader.ReadAsync();
        Assert.AreEqual("query-executing", evt.EventType);
    }

    [TestMethod(DisplayName = "OnCompletedAsync_BroadcastsQueryCompletedEvent")]
    public async Task OnCompletedAsync_BroadcastsQueryCompletedEvent()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);
        var result = new DatabaseResult("srv", "db", DateTime.UtcNow, "Success", 10, string.Empty, 100, ["id"], []);

        await sink.OnCompletedAsync(result);

        var channel = _broadcaster.GetChannel(_executionId);
        var evt = await channel!.Reader.ReadAsync();
        Assert.AreEqual("query-completed", evt.EventType);
    }

    [TestMethod(DisplayName = "OnFailedAsync_BroadcastsQueryFailedEvent")]
    public async Task OnFailedAsync_BroadcastsQueryFailedEvent()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);

        await sink.OnFailedAsync(new DatabaseError("srv", "db", DateTime.UtcNow, "timeout"));

        var channel = _broadcaster.GetChannel(_executionId);
        var evt = await channel!.Reader.ReadAsync();
        Assert.AreEqual("query-failed", evt.EventType);
    }

    [TestMethod(DisplayName = "OnFinishedAsync_BroadcastsExecutionCompletedAndCompletesChannel")]
    public async Task OnFinishedAsync_BroadcastsExecutionCompletedAndCompletesChannel()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);

        await sink.OnFinishedAsync(new ExecutionSummary(5, 1, 1000, "/output"));

        var channel = _broadcaster.GetChannel(_executionId);
        var evt = await channel!.Reader.ReadAsync();
        Assert.AreEqual("execution-completed", evt.EventType);
        Assert.IsTrue(channel.Reader.Completion.IsCompleted);
    }

    [TestMethod(DisplayName = "OnStartedAsync_UpdatesStatusTracker")]
    public async Task OnStartedAsync_UpdatesStatusTracker()
    {
        var sink = new SseEventSinkService(_executionId, _broadcaster, _statusTracker);

        await sink.OnStartedAsync(new QueryExecutionInfo(_executionId.ToString(), 2, 5, DateTime.UtcNow));

        var status = _statusTracker.GetStatus(_executionId);
        Assert.IsNotNull(status);
        Assert.AreEqual(5, status.TotalDatabases);
    }
}
