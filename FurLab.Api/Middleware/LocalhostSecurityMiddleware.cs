namespace FurLab.Api.Middleware;

/// <summary>
/// Middleware that rejects requests from non-loopback IP addresses.
/// Provides defense-in-depth alongside Kestrel's localhost bind.
/// </summary>
public class LocalhostSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalhostSecurityMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalhostSecurityMiddleware"/> class.
    /// </summary>
    public LocalhostSecurityMiddleware(RequestDelegate next, ILogger<LocalhostSecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware. Allows only loopback (localhost) connections.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            _logger.LogWarning("Rejecting request with unknown remote IP address");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: unable to determine client IP address.");
            return;
        }

        if (!System.Net.IPAddress.IsLoopback(remoteIp))
        {
            _logger.LogWarning("Rejecting request from non-loopback IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: this API is only accessible from localhost.");
            return;
        }

        await _next(context);
    }
}
