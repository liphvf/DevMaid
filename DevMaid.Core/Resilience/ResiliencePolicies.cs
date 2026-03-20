using System;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace DevMaid.Core.Resilience;

public static class ResiliencePolicies
{
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
