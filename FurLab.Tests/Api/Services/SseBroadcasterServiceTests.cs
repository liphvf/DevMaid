using FurLab.Api.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace FurLab.Tests.Api.Services;

[TestClass]
public class SseBroadcasterServiceTests
{
    private SseBroadcasterService _broadcaster = null!;
    private readonly Guid _executionId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _broadcaster = new SseBroadcasterService(NullLogger<SseBroadcasterService>.Instance);
    }

    [TestMethod(DisplayName = "CreateChannel_ReturnsWritableChannel")]
    public void CreateChannel_ReturnsWritableChannel()
    {
        var channel = _broadcaster.CreateChannel(_executionId);

        Assert.IsNotNull(channel);
        Assert.IsTrue(channel.Writer.TryWrite(new SseEvent("test", "data")));
    }

    [TestMethod(DisplayName = "BroadcastAsync_ExistingChannel_WritesEvent")]
    public async Task BroadcastAsync_ExistingChannel_WritesEvent()
    {
        _broadcaster.CreateChannel(_executionId);
        var evt = new SseEvent("test", "data");

        await _broadcaster.BroadcastAsync(_executionId, evt);

        var channel = _broadcaster.GetChannel(_executionId);
        var received = await channel!.Reader.ReadAsync();
        Assert.AreEqual("test", received.EventType);
        Assert.AreEqual("data", received.Data);
    }

    [TestMethod(DisplayName = "BroadcastAsync_NonExistingChannel_DoesNotThrow")]
    public async Task BroadcastAsync_NonExistingChannel_DoesNotThrow()
    {
        var evt = new SseEvent("test", "data");

        await _broadcaster.BroadcastAsync(Guid.NewGuid(), evt);
    }

    [TestMethod(DisplayName = "CompleteChannel_MakesChannelComplete")]
    public void CompleteChannel_MakesChannelComplete()
    {
        _broadcaster.CreateChannel(_executionId);

        _broadcaster.CompleteChannel(_executionId);

        var channel = _broadcaster.GetChannel(_executionId);
        Assert.IsTrue(channel!.Reader.Completion.IsCompleted);
    }

    [TestMethod(DisplayName = "RemoveChannel_RemovesChannel")]
    public void RemoveChannel_RemovesChannel()
    {
        _broadcaster.CreateChannel(_executionId);

        _broadcaster.RemoveChannel(_executionId);

        Assert.IsNull(_broadcaster.GetChannel(_executionId));
    }
}
