using System;

namespace DevMaid.Services.Logging;

/// <summary>
/// Console-based implementation of the <see cref="ILogger"/> interface.
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly bool _useColors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
    /// </summary>
    /// <param name="useColors">Whether to use colors in the console output.</param>
    public ConsoleLogger(bool useColors = true)
    {
        _useColors = useColors;
    }

    /// <inheritdoc/>
    public void LogInformation(string message, params object[] args)
    {
        if (_useColors)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {message}", args);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine($"[INFO] {message}", args);
        }
    }

    /// <inheritdoc/>
    public void LogWarning(string message, params object[] args)
    {
        if (_useColors)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {message}", args);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine($"[WARN] {message}", args);
        }
    }

    /// <inheritdoc/>
    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        if (_useColors)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}", args);
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
                Console.WriteLine($"Stack Trace: {exception.StackTrace}");
            }
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine($"[ERROR] {message}", args);
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
                Console.WriteLine($"Stack Trace: {exception.StackTrace}");
            }
        }
    }

    /// <inheritdoc/>
    public void LogDebug(string message, params object[] args)
    {
#if DEBUG
        if (_useColors)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[DEBUG] {message}", args);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine($"[DEBUG] {message}", args);
        }
#endif
    }
}

/// <summary>
/// Provides a static logger instance for use throughout the application.
/// </summary>
public static class Logger
{
    private static ILogger? _instance;

    /// <summary>
    /// Gets or sets the current logger instance.
    /// </summary>
    public static ILogger Instance
    {
        get => _instance ?? new ConsoleLogger();
        set => _instance = value;
    }

    /// <summary>
    /// Sets the logger instance to use.
    /// </summary>
    /// <param name="logger">The logger to set.</param>
    public static void SetLogger(ILogger logger)
    {
        _instance = logger;
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public static void LogInformation(string message, params object[] args)
    {
        Instance.LogInformation(message, args);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void LogWarning(string message, params object[] args)
    {
        Instance.LogWarning(message, args);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void LogError(string message, Exception? exception = null, params object[] args)
    {
        Instance.LogError(message, exception, args);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public static void LogDebug(string message, params object[] args)
    {
        Instance.LogDebug(message, args);
    }
}
