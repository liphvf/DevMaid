using FurLab.Api.Controllers;
using FurLab.Api.Services;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Models.Api;
using FurLab.Core.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.Tests.Api.Controllers;

[TestClass]
public class QueryControllerTests
{
    private Mock<IUserConfigService> _userConfigMock = null!;
    private QueryPlannerService _queryPlanner = null!;
    private QueryExecutorService _queryExecutor = null!;
    private ExecutionRegistryService _executionRegistry = null!;
    private SseBroadcasterService _sseBroadcaster = null!;
    private StatusTrackerService _statusTracker = null!;
    private CsvExporterService _csvExporter = null!;
    private QueryController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _userConfigMock = new Mock<IUserConfigService>();
        _userConfigMock.Setup(x => x.GetDefaults()).Returns(new UserDefaults
        {
            OutputDirectory = Path.Combine(Path.GetTempPath(), "furlab-tests"),
            RequireConfirmation = true
        });

        var credentialMock = new Mock<ICredentialService>();
        credentialMock.Setup(x => x.TryDecrypt(It.IsAny<string?>())).Returns("password123");

        _queryPlanner = new QueryPlannerService(credentialMock.Object, NullLogger<QueryPlannerService>.Instance);
        _queryExecutor = new QueryExecutorService(
            _queryPlanner,
            new CsvExporterService(),
            NullLogger<QueryExecutorService>.Instance);

        _executionRegistry = new ExecutionRegistryService(NullLogger<ExecutionRegistryService>.Instance);
        _sseBroadcaster = new SseBroadcasterService(NullLogger<SseBroadcasterService>.Instance);
        _statusTracker = new StatusTrackerService();
        _csvExporter = new CsvExporterService();

        _controller = new QueryController(
            _userConfigMock.Object,
            _queryPlanner,
            _queryExecutor,
            _executionRegistry,
            _sseBroadcaster,
            _statusTracker,
            _csvExporter,
            NullLogger<QueryController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [TestMethod(DisplayName = "Analyze_SelectQuery_ReturnsNonDestructive")]
    public void Analyze_SelectQuery_ReturnsNonDestructive()
    {
        _userConfigMock.Setup(x => x.GetServers()).Returns([]);

        var result = _controller.Analyze(new QueryAnalyzeRequest("SELECT * FROM users", []));

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var response = okResult.Value as QueryAnalyzeResponse;
        Assert.IsNotNull(response);
        Assert.IsFalse(response.IsDestructive);
        Assert.AreEqual("SELECT", response.QueryType);
    }

    [TestMethod(DisplayName = "Analyze_DeleteQuery_ReturnsDestructive")]
    public void Analyze_DeleteQuery_ReturnsDestructive()
    {
        _userConfigMock.Setup(x => x.GetServers()).Returns([]);

        var result = _controller.Analyze(new QueryAnalyzeRequest("DELETE FROM users", []));

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var response = okResult.Value as QueryAnalyzeResponse;
        Assert.IsNotNull(response);
        Assert.IsTrue(response.IsDestructive);
        Assert.IsTrue(response.RequiresConfirmation);
    }

    [TestMethod(DisplayName = "Execute_DestructiveWithoutConfirmation_ReturnsBadRequest")]
    public async Task Execute_DestructiveWithoutConfirmation_ReturnsBadRequest()
    {
        _userConfigMock.Setup(x => x.GetServers()).Returns([
            new ServerConfigEntry { Name = "srv", Host = "localhost", Port = 5432, Username = "user", Databases = ["db1"] }
        ]);

        var result = await _controller.Execute(new QueryExecuteRequest("DELETE FROM users", ["srv"]) { Confirmed = false });

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
    }

    [TestMethod(DisplayName = "Execute_ValidRequest_ReturnsExecutionId")]
    public async Task Execute_ValidRequest_ReturnsExecutionId()
    {
        _userConfigMock.Setup(x => x.GetServers()).Returns([
            new ServerConfigEntry { Name = "srv", Host = "localhost", Port = 5432, Username = "user", Databases = ["db1"] }
        ]);

        var result = await _controller.Execute(new QueryExecuteRequest("SELECT 1", ["srv"]) { Confirmed = true });

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var response = okResult.Value as QueryExecuteResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("started", response.Status);
        Assert.IsFalse(string.IsNullOrEmpty(response.ExecutionId));

        // Cleanup: cancel the background execution
        var executionId = Guid.Parse(response.ExecutionId);
        _controller.Cancel(executionId);
    }

    [TestMethod(DisplayName = "Execute_NoServersConfigured_ReturnsBadRequest")]
    public async Task Execute_NoServersConfigured_ReturnsBadRequest()
    {
        _userConfigMock.Setup(x => x.GetServers()).Returns([]);

        var result = await _controller.Execute(new QueryExecuteRequest("SELECT 1", []) { Confirmed = true });

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
    }

    [TestMethod(DisplayName = "Status_ExistingExecution_ReturnsStatus")]
    public void Status_ExistingExecution_ReturnsStatus()
    {
        var executionId = Guid.NewGuid();
        _statusTracker.Initialize(executionId, new QueryExecutionInfo(executionId.ToString(), 1, 3, DateTime.UtcNow));

        var result = _controller.Status(executionId);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var response = okResult.Value as QueryStatusResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual(executionId.ToString(), response.ExecutionId);
        Assert.AreEqual("running", response.Status);
    }

    [TestMethod(DisplayName = "Status_NonExisting_Returns404")]
    public void Status_NonExisting_Returns404()
    {
        var result = _controller.Status(Guid.NewGuid());

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
    }

    [TestMethod(DisplayName = "Cancel_ExistingRunningExecution_ReturnsCancelled")]
    public void Cancel_ExistingRunningExecution_ReturnsCancelled()
    {
        var executionId = Guid.NewGuid();
        var context = new ExecutionContext { CancellationTokenSource = new CancellationTokenSource() };
        _executionRegistry.Register(context);
        // Reflection to set the ExecutionId since Register generates a new one
        var registeredId = _executionRegistry.GetAllIds().First();

        var result = _controller.Cancel(registeredId);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
    }

    [TestMethod(DisplayName = "Cancel_NonExisting_Returns404")]
    public void Cancel_NonExisting_Returns404()
    {
        var result = _controller.Cancel(Guid.NewGuid());

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
    }

    [TestMethod(DisplayName = "Cancel_AlreadyCancelled_ReturnsConflict")]
    public void Cancel_AlreadyCancelled_ReturnsConflict()
    {
        var executionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = new ExecutionContext { CancellationTokenSource = cts };
        _executionRegistry.Register(context);
        var registeredId = _executionRegistry.GetAllIds().First();

        var result = _controller.Cancel(registeredId);

        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict);
    }

    [TestMethod(DisplayName = "Download_ExecutionNotFound_Returns404")]
    public void Download_ExecutionNotFound_Returns404()
    {
        var result = _controller.Download(Guid.NewGuid(), "consolidated");

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
    }
}
