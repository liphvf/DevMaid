using FurLab.Api.Services;

using Microsoft.Extensions.Logging.Abstractions;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.Tests.Api.Services;

[TestClass]
public class ExecutionRegistryServiceTests
{
    private ExecutionRegistryService _registry = null!;

    [TestInitialize]
    public void Setup()
    {
        _registry = new ExecutionRegistryService(NullLogger<ExecutionRegistryService>.Instance);
    }

    [TestMethod(DisplayName = "Register_ReturnsNewGuid")]
    public void Register_ReturnsNewGuid()
    {
        var context = new ExecutionContext { SqlQuery = "SELECT 1" };

        var id = _registry.Register(context);

        Assert.AreNotEqual(Guid.Empty, id);
    }

    [TestMethod(DisplayName = "Get_ExistingId_ReturnsContext")]
    public void Get_ExistingId_ReturnsContext()
    {
        var context = new ExecutionContext { SqlQuery = "SELECT 1" };
        var id = _registry.Register(context);

        var result = _registry.Get(id);

        Assert.IsNotNull(result);
        Assert.AreEqual("SELECT 1", result.SqlQuery);
    }

    [TestMethod(DisplayName = "Get_NonExistingId_ReturnsNull")]
    public void Get_NonExistingId_ReturnsNull()
    {
        var result = _registry.Get(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "Remove_ExistingId_ReturnsTrueAndRemoves")]
    public void Remove_ExistingId_ReturnsTrueAndRemoves()
    {
        var context = new ExecutionContext { SqlQuery = "SELECT 1" };
        var id = _registry.Register(context);

        var removed = _registry.Remove(id);
        var afterRemove = _registry.Get(id);

        Assert.IsTrue(removed);
        Assert.IsNull(afterRemove);
    }

    [TestMethod(DisplayName = "Remove_NonExistingId_ReturnsFalse")]
    public void Remove_NonExistingId_ReturnsFalse()
    {
        var removed = _registry.Remove(Guid.NewGuid());

        Assert.IsFalse(removed);
    }

    [TestMethod(DisplayName = "Register_MultipleExecutions_ReturnsDifferentGuids")]
    public void Register_MultipleExecutions_ReturnsDifferentGuids()
    {
        var id1 = _registry.Register(new ExecutionContext { SqlQuery = "SELECT 1" });
        var id2 = _registry.Register(new ExecutionContext { SqlQuery = "SELECT 2" });

        Assert.AreNotEqual(id1, id2);
        Assert.AreEqual(2, _registry.GetAllIds().Count());
    }
}
