
namespace DevMaid.Core.Logging;

/// <summary>
/// Console-based implementation of the <see cref="ILogger"/> interface.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
/// </remarks>
/// <param name="useColors">Whether to use colors in the console output.</param>
public class ConsoleLogger(bool useColors = true) : ILogger
{
    private readonly bool _useColors = useColors;

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
