using FurLab.Api.Services;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Models.Api;
using FurLab.Core.Services;
using Microsoft.AspNetCore.Mvc;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.Api.Controllers;

/// <summary>
/// API controller for SQL query execution against PostgreSQL servers.
/// </summary>
[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly IUserConfigService _userConfigService;
    private readonly QueryPlannerService _queryPlanner;
    private readonly QueryExecutorService _queryExecutor;
    private readonly ExecutionRegistryService _executionRegistry;
    private readonly SseBroadcasterService _sseBroadcaster;
    private readonly StatusTrackerService _statusTracker;
    private readonly CsvExporterService _csvExporter;
    private readonly ILogger<QueryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryController"/> class.
    /// </summary>
    public QueryController(
        IUserConfigService userConfigService,
        QueryPlannerService queryPlanner,
        QueryExecutorService queryExecutor,
        ExecutionRegistryService executionRegistry,
        SseBroadcasterService sseBroadcaster,
        StatusTrackerService statusTracker,
        CsvExporterService csvExporter,
        ILogger<QueryController> logger)
    {
        _userConfigService = userConfigService;
        _queryPlanner = queryPlanner;
        _queryExecutor = queryExecutor;
        _executionRegistry = executionRegistry;
        _sseBroadcaster = sseBroadcaster;
        _statusTracker = statusTracker;
        _csvExporter = csvExporter;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a SQL query and returns its type, destructiveness, and estimated impact.
    /// </summary>
    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] QueryAnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new { error = "SQL query is required." });
        }

        var queryType = SqlQueryAnalyzerService.AnalyzeQuery(request.Sql);
        var queryTypeDescription = SqlQueryAnalyzerService.GetQueryTypeDescription(request.Sql);

        var servers = request.Servers ?? [];
        var selectedServers = ResolveServers(servers);
        var affectedServers = selectedServers.Count;
        var estimatedDatabases = selectedServers.Sum(s => s.FetchAllDatabases ? 1 : Math.Max(s.Databases.Count, 1));

        var isDestructive = queryType == QueryType.Destructive;
        var requiresConfirmation = isDestructive && _userConfigService.GetDefaults().RequireConfirmation;

        return Ok(new QueryAnalyzeResponse(
            queryTypeDescription,
            isDestructive,
            affectedServers,
            estimatedDatabases,
            requiresConfirmation));
    }

    /// <summary>
    /// Initiates query execution across configured servers and returns an execution identifier.
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] QueryExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new { error = "SQL query is required." });
        }

        var queryType = SqlQueryAnalyzerService.AnalyzeQuery(request.Sql);
        if (queryType == QueryType.Destructive && !request.Confirmed)
        {
            return BadRequest(new { error = "Destructive query requires confirmation. Use /analyze first, then set confirmed: true." });
        }

        var servers = request.Servers ?? [];
        var selectedServers = ResolveServers(servers);

        if (selectedServers.Count == 0)
        {
            return BadRequest(new { error = "No servers configured or selected." });
        }

        if (request.AllDatabases)
        {
            foreach (var server in selectedServers)
            {
                server.FetchAllDatabases = true;
            }
        }

        var excludeNames = ParseExcludeList(request.Exclude);

        var defaults = _userConfigService.GetDefaults();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var executionDirectory = Path.Combine(defaults.OutputDirectory, timestamp);

        if (!Directory.Exists(executionDirectory))
        {
            Directory.CreateDirectory(executionDirectory);
        }

        var allDatabases = new List<(ServerConfigEntry Server, string Database)>();
        foreach (var server in selectedServers)
        {
            var databases = await _queryPlanner.GetDatabasesForServerAsync(server, excludeNames, HttpContext.RequestAborted);
            foreach (var db in databases)
            {
                allDatabases.Add((server, db));
            }
        }

        if (allDatabases.Count == 0)
        {
            return BadRequest(new { error = "No accessible databases found across selected servers." });
        }

        var executionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        var executionContext = new ExecutionContext
        {
            ExecutionId = executionId,
            SqlQuery = request.Sql,
            QuerySource = "api",
            ServerDatabases = allDatabases,
            OutputDirectory = executionDirectory,
            Timestamp = timestamp,
            QueryTypeDescription = SqlQueryAnalyzerService.GetQueryTypeDescription(request.Sql),
            IsDestructive = queryType == QueryType.Destructive,
            CommandTimeout = 300,
            Settings = new QueryExecutionSettings
            {
                All = request.AllDatabases,
                Exclude = request.Exclude
            },
            CancellationTokenSource = cts
        };

        _executionRegistry.Register(executionContext);
        _sseBroadcaster.CreateChannel(executionId);

        var sseSink = new SseEventSinkService(executionId, _sseBroadcaster, _statusTracker);

        _ = Task.Run(async () =>
        {
            try
            {
                await _queryExecutor.ExecuteAsync(executionContext, sseSink, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution {ExecutionId} failed", executionId);
                _sseBroadcaster.CompleteChannel(executionId);
            }
            finally
            {
                _sseBroadcaster.RemoveChannel(executionId);
            }
        }, cts.Token);

        return Ok(new QueryExecuteResponse(
            executionId.ToString(),
            "started",
            executionDirectory));
    }

    /// <summary>
    /// Returns the current status of a query execution.
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status([FromQuery] Guid executionId)
    {
        var status = _statusTracker.GetStatus(executionId);
        if (status == null)
        {
            return NotFound(new { error = "Execution not found." });
        }

        var results = status.Results.Select(r => new DatabaseResultResponse(
            r.Server,
            r.Database,
            r.Status,
            r.RowCount,
            r.DurationMs,
            string.IsNullOrEmpty(r.Error) ? null : r.Error)).ToList();

        return Ok(new QueryStatusResponse(
            executionId.ToString(),
            status.Status,
            status.TotalDatabases,
            status.Completed,
            status.Failed,
            status.InProgress,
            status.StartedAt,
            status.CompletedAt,
            status.OutputDirectory,
            results));
    }

    /// <summary>
    /// Streams Server-Sent Events for real-time query execution progress.
    /// </summary>
    [HttpGet("events")]
    public async IAsyncEnumerable<SseEvent> Events([FromQuery] Guid executionId)
    {
        var channel = _sseBroadcaster.GetChannel(executionId);
        if (channel == null)
        {
            yield return new SseEvent("error", "{ \"message\": \"Execution not found or already completed.\" }");
            yield break;
        }

        await foreach (var @event in channel.Reader.ReadAllAsync())
        {
            yield return @event;
        }
    }

    /// <summary>
    /// Cancels a running query execution.
    /// </summary>
    [HttpPost("cancel")]
    public IActionResult Cancel([FromQuery] Guid executionId)
    {
        var execution = _executionRegistry.Get(executionId);
        if (execution == null)
        {
            return NotFound(new { error = "Execution not found." });
        }

        if (execution.CancellationTokenSource.IsCancellationRequested)
        {
            return Conflict(new { error = "Execution is already cancelled or completed." });
        }

        execution.CancellationTokenSource.Cancel();
        return Ok(new QueryCancelResponse(executionId.ToString(), "cancelled"));
    }

    /// <summary>
    /// Downloads a result CSV file from a completed execution.
    /// </summary>
    [HttpGet("download")]
    public IActionResult Download([FromQuery] Guid executionId, [FromQuery] string type = "consolidated")
    {
        var execution = _executionRegistry.Get(executionId);
        if (execution == null)
        {
            return NotFound(new { error = "Execution not found." });
        }

        string filePath;
        string fileName;

        switch (type.ToLowerInvariant())
        {
            case "consolidated":
                fileName = $"consolidated_{execution.Timestamp}.csv";
                filePath = Path.Combine(execution.OutputDirectory, fileName);
                break;
            case "log":
                fileName = $"{execution.Timestamp}_log.csv";
                filePath = Path.Combine(execution.OutputDirectory, fileName);
                break;
            default:
                return BadRequest(new { error = "Invalid type. Use 'consolidated' or 'log'." });
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "File not found." });
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "text/csv", fileName);
    }

    private List<ServerConfigEntry> ResolveServers(List<string> requestedServers)
    {
        var allServers = _userConfigService.GetServers();

        if (requestedServers.Count == 0)
        {
            return allServers.ToList();
        }

        return allServers.Where(s => requestedServers.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static HashSet<string> ParseExcludeList(string? exclude)
    {
        if (string.IsNullOrWhiteSpace(exclude))
        {
            return [];
        }

        return new HashSet<string>(
            exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }
}
