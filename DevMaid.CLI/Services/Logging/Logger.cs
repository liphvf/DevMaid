namespace DevMaid.Services.Logging;

/// <summary>
/// Static logger class for backward compatibility.
/// </summary>
public static class Logger
{
    private static Core.Logging.ILogger? _logger;

    /// <summary>
    /// Sets the logger instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public static void SetLogger(Core.Logging.ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current logger instance.
    /// </summary>
    public static Core.Logging.ILogger Instance => _logger ?? throw new InvalidOperationException("Logger not set");

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogDebug(string message)
    {
        _logger?.LogDebug(message);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogInformation(string message)
    {
        _logger?.LogInformation(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogWarning(string message)
    {
        _logger?.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogError(string message)
    {
        _logger?.LogError(message);
    }

    /// <summary>
    /// Logs a formatted informational message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogInformation(string message, params object[] args)
    {
        _logger?.LogInformation(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted debug message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogDebug(string message, params object[] args)
    {
        _logger?.LogDebug(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted warning message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogWarning(string message, params object[] args)
    {
        _logger?.LogWarning(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted error message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogError(string message, params object[] args)
    {
        _logger?.LogError(string.Format(message, args));
    }
}
