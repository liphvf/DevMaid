namespace FurLab.Core.Logging;

/// <summary>
/// Provides correlation context for tracking operations across the application.
/// </summary>
public class CorrelationContext
{
    private static readonly AsyncLocal<CorrelationContext?> CurrentContext = new();

    /// <summary>
    /// Gets the unique correlation identifier for this context.
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Gets the UTC timestamp when this context was created.
    /// </summary>
    public DateTime StartTimeUtc { get; }

    /// <summary>
    /// Gets or sets the name of the command being executed.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the dictionary of additional properties associated with this context.
    /// </summary>
    public Dictionary<string, object> Properties { get; } = [];

    private CorrelationContext(string correlationId)
    {
        CorrelationId = correlationId;
        StartTimeUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new correlation context with an optional correlation ID.
    /// </summary>
    /// <param name="correlationId">Optional correlation ID. If null, a new GUID is generated.</param>
    /// <returns>A new CorrelationContext instance.</returns>
    public static CorrelationContext Create(string? correlationId = null)
    {
        var context = new CorrelationContext(correlationId ?? Guid.NewGuid().ToString("N")[..8]);
        CurrentContext.Value = context;
        return context;
    }

    /// <summary>
    /// Gets the current correlation context for the async flow.
    /// </summary>
    public static CorrelationContext? Current => CurrentContext.Value;

    /// <summary>
    /// Sets a property in the context properties dictionary.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// Gets the elapsed time since this context was created.
    /// </summary>
    public TimeSpan Elapsed => DateTime.UtcNow - StartTimeUtc;
}
