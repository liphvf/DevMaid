using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DevMaid.Core.Logging;

public class CorrelationContext
{
    private static readonly AsyncLocal<CorrelationContext?> CurrentContext = new();

    public string CorrelationId { get; }
    public DateTime StartTimeUtc { get; }
    public string CommandName { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; } = new();

    private CorrelationContext(string correlationId)
    {
        CorrelationId = correlationId;
        StartTimeUtc = DateTime.UtcNow;
    }

    public static CorrelationContext Create(string? correlationId = null)
    {
        var context = new CorrelationContext(correlationId ?? Guid.NewGuid().ToString("N")[..8]);
        CurrentContext.Value = context;
        return context;
    }

    public static CorrelationContext? Current => CurrentContext.Value;

    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    public TimeSpan Elapsed => DateTime.UtcNow - StartTimeUtc;
}

public static class LoggerExtensions
{
    public static void LogWithCorrelation(this Microsoft.Extensions.Logging.ILogger logger, LogLevel logLevel, string message, params object[] args)
    {
        var correlation = CorrelationContext.Current;
        if (correlation != null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlation.CorrelationId,
                ["CommandName"] = correlation.CommandName
            }))
            {
                logger.Log(logLevel, message, args);
            }
        }
        else
        {
            logger.Log(logLevel, message, args);
        }
    }

    public static IDisposable BeginCorrelationScope(this Microsoft.Extensions.Logging.ILogger logger, string commandName)
    {
        var correlation = CorrelationContext.Current;
        if (correlation != null)
        {
            correlation.CommandName = commandName;
        }

        var correlationId = correlation?.CorrelationId ?? "no-correlation";
        return new CorrelationScopeWrapper(logger, correlationId, commandName);
    }

    private sealed class CorrelationScopeWrapper : IDisposable
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public CorrelationScopeWrapper(Microsoft.Extensions.Logging.ILogger logger, string correlationId, string commandName)
        {
            _logger = logger;
            _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["CommandName"] = commandName
            });
        }

        public void Dispose()
        {
        }
    }
}
