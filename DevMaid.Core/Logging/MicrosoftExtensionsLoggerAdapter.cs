using Microsoft.Extensions.Logging;

namespace DevMaid.Core.Logging;

/// <summary>
/// Adapter for Microsoft.Extensions.Logging.ILogger to DevMaid.Core.Logging.ILogger.
/// </summary>
public sealed class MicrosoftExtensionsLoggerAdapter : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public MicrosoftExtensionsLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        if (exception != null)
        {
            _logger.LogError(exception, message, args);
        }
        else
        {
            _logger.LogError(message, args);
        }
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }
}
