
namespace DevMaid.CLI.Services.Logging;

/// <summary>
/// Console logger implementation.
/// </summary>
public class ConsoleLogger : Core.Logging.ILogger
{
    private readonly bool _useColors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
    /// </summary>
    /// <param name="useColors">Whether to use colors in the output.</param>
    public ConsoleLogger(bool useColors = true)
    {
        _useColors = useColors;
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public void LogDebug(string message, params object[] args)
    {
        if (_useColors)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        Console.WriteLine($"[DEBUG] {formattedMessage}");

        if (_useColors)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public void LogInformation(string message, params object[] args)
    {
        if (_useColors)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }

        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        Console.WriteLine($"[INFO] {formattedMessage}");

        if (_useColors)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public void LogWarning(string message, params object[] args)
    {
        if (_useColors)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        Console.WriteLine($"[WARN] {formattedMessage}");

        if (_useColors)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception associated with the error.</param>
    /// <param name="args">Optional format arguments.</param>
    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        if (_useColors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        Console.WriteLine($"[ERROR] {formattedMessage}");

        if (exception != null)
        {
            Console.WriteLine($"[ERROR] Exception: {exception.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {exception.StackTrace}");
        }

        if (_useColors)
        {
            Console.ResetColor();
        }
    }
}
