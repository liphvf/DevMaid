namespace DevMaid.CLI.Services.Logging;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Static logger class for backward compatibility.
/// </summary>
public static class Logger
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the current logger instance.
    /// </summary>
    private static Core.Logging.ILogger? GetLogger()
    {
        if (_serviceProvider == null)
        {
            return null;
        }

        try
        {
            var msLoggerFactory = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var msLogger = msLoggerFactory.CreateLogger("DevMaid");
            return new Core.Logging.MicrosoftExtensionsLoggerAdapter(msLogger);
        }
        catch (Exception ex)
        {
            // For production-readiness, do not silently swallow DI resolution errors.
            Console.Error.WriteLine($"[DevMaid.CLI Logger] Failed to initialize logger from ServiceProvider: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the current logger instance.
    /// </summary>
    public static Core.Logging.ILogger Instance => GetLogger() ?? throw new InvalidOperationException("Logger not initialized. Call SetServiceProvider first.");

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogDebug(string message)
    {
        GetLogger()?.LogDebug(message);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogInformation(string message)
    {
        GetLogger()?.LogInformation(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogWarning(string message)
    {
        GetLogger()?.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogError(string message)
    {
        GetLogger()?.LogError(message);
    }

    /// <summary>
    /// Logs a formatted informational message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogInformation(string message, params object[] args)
    {
        GetLogger()?.LogInformation(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted debug message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogDebug(string message, params object[] args)
    {
        GetLogger()?.LogDebug(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted warning message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogWarning(string message, params object[] args)
    {
        GetLogger()?.LogWarning(string.Format(message, args));
    }

    /// <summary>
    /// Logs a formatted error message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The arguments to format.</param>
    public static void LogError(string message, params object[] args)
    {
        GetLogger()?.LogError(string.Format(message, args));
    }
}
