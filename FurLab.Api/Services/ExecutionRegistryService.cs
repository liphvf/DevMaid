using System.Collections.Concurrent;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.Api.Services;

/// <summary>
/// Registry for tracking active query executions by their unique identifier.
/// Supports multiple simultaneous executions.
/// </summary>
public class ExecutionRegistryService
{
    private readonly ConcurrentDictionary<Guid, ExecutionContext> _executions = new();
    private readonly ILogger<ExecutionRegistryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionRegistryService"/> class.
    /// </summary>
    public ExecutionRegistryService(ILogger<ExecutionRegistryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a new execution context and returns its unique identifier.
    /// </summary>
    public Guid Register(ExecutionContext context)
    {
        var id = Guid.NewGuid();
        _executions[id] = context;
        _logger.LogInformation("Registered execution {ExecutionId} for {ServerCount} servers", id, context.ServerDatabases.Count);
        return id;
    }

    /// <summary>
    /// Attempts to retrieve an execution context by its identifier.
    /// </summary>
    public ExecutionContext? Get(Guid executionId)
    {
        _executions.TryGetValue(executionId, out var context);
        return context;
    }

    /// <summary>
    /// Removes an execution context from the registry.
    /// </summary>
    public bool Remove(Guid executionId)
    {
        var removed = _executions.TryRemove(executionId, out _);
        if (removed)
        {
            _logger.LogInformation("Removed execution {ExecutionId} from registry", executionId);
        }
        return removed;
    }

    /// <summary>
    /// Gets all registered execution identifiers.
    /// </summary>
    public IEnumerable<Guid> GetAllIds() => _executions.Keys;
}
