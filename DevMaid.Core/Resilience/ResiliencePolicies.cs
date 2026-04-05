
using Polly;
using Polly.Retry;

namespace DevMaid.Core.Resilience;

/// <summary>
/// Provides resilience pipeline policies for handling transient failures.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a resilience pipeline with retry logic for database operations.
    /// </summary>
    /// <param name="onRetry">Optional callback invoked on each retry attempt.</param>
    /// <returns>A resilience pipeline configured for database retry scenarios.</returns>
    public static ResiliencePipeline CreateDatabaseRetryPipeline(
        Action<string> onRetry = null!)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("pg_dump", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("pg_restore", StringComparison.OrdinalIgnoreCase)),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    onRetry?.Invoke($"Retry {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s due to: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Creates a resilience pipeline with retry logic for file operations.
    /// </summary>
    /// <param name="onRetry">Optional callback invoked on each retry attempt.</param>
    /// <returns>A resilience pipeline configured for file operation retry scenarios.</returns>
    public static ResiliencePipeline CreateFileOperationPipeline(
        Action<string> onRetry = null!)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase)),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    onRetry?.Invoke($"Retry {args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds}ms due to: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
