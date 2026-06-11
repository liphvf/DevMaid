using FurLab.Api.Services;
using FurLab.Core.Models;

namespace FurLab.Tests.Api.Services;

[TestClass]
public class StatusTrackerServiceTests
{
    private StatusTrackerService _tracker = null!;
    private readonly Guid _executionId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _tracker = new StatusTrackerService();
    }

    [TestMethod(DisplayName = "Initialize_SetsStatusToRunning")]
    public void Initialize_SetsStatusToRunning()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 2, 5, DateTime.UtcNow));

        var status = _tracker.GetStatus(_executionId);

        Assert.IsNotNull(status);
        Assert.AreEqual("running", status.Status);
        Assert.AreEqual(5, status.TotalDatabases);
    }

    [TestMethod(DisplayName = "MarkExecuting_IncrementsInProgress")]
    public void MarkExecuting_IncrementsInProgress()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 1, 3, DateTime.UtcNow));

        _tracker.MarkExecuting(_executionId, new DatabaseExecutionInfo("srv", "db", DateTime.UtcNow));

        var status = _tracker.GetStatus(_executionId);
        Assert.AreEqual(1, status!.InProgress);
    }

    [TestMethod(DisplayName = "MarkCompleted_IncrementsCompletedAndAddsResult")]
    public void MarkCompleted_IncrementsCompletedAndAddsResult()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 1, 3, DateTime.UtcNow));

        _tracker.MarkCompleted(_executionId, new DatabaseResult("srv", "db", DateTime.UtcNow, "Success", 100, string.Empty, 250, ["id"], []));

        var status = _tracker.GetStatus(_executionId);
        Assert.AreEqual(1, status!.Completed);
        Assert.AreEqual(100, status.TotalRows);
        Assert.AreEqual(1, status.Results.Count);
    }

    [TestMethod(DisplayName = "MarkFailed_IncrementsFailed")]
    public void MarkFailed_IncrementsFailed()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 1, 3, DateTime.UtcNow));

        _tracker.MarkFailed(_executionId, new DatabaseError("srv", "db", DateTime.UtcNow, "timeout"));

        var status = _tracker.GetStatus(_executionId);
        Assert.AreEqual(1, status!.Failed);
        Assert.AreEqual(0, status.TotalRows);
    }

    [TestMethod(DisplayName = "MarkFinished_SetsStatusToCompleted")]
    public void MarkFinished_SetsStatusToCompleted()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 1, 1, DateTime.UtcNow));

        _tracker.MarkFinished(_executionId, new ExecutionSummary(1, 0, 50, "/output"));

        var status = _tracker.GetStatus(_executionId);
        Assert.AreEqual("completed", status!.Status);
        Assert.IsNotNull(status.CompletedAt);
        Assert.AreEqual("/output", status.OutputDirectory);
    }

    [TestMethod(DisplayName = "GetStatus_NonExisting_ReturnsNull")]
    public void GetStatus_NonExisting_ReturnsNull()
    {
        var status = _tracker.GetStatus(Guid.NewGuid());

        Assert.IsNull(status);
    }

    [TestMethod(DisplayName = "Remove_DeletesStatus")]
    public void Remove_DeletesStatus()
    {
        _tracker.Initialize(_executionId, new QueryExecutionInfo(_executionId.ToString(), 1, 1, DateTime.UtcNow));

        _tracker.Remove(_executionId);

        Assert.IsNull(_tracker.GetStatus(_executionId));
    }
}
