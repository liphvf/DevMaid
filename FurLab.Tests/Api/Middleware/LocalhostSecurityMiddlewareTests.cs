using System.Net;

using FurLab.Api.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FurLab.Tests.Api.Middleware;

[TestClass]
public class LocalhostSecurityMiddlewareTests
{
    private static RequestDelegate _next = _ => Task.CompletedTask;

    [TestMethod(DisplayName = "InvokeAsync_LoopbackIp_CallsNext")]
    public async Task InvokeAsync_LoopbackIp_CallsNext()
    {
        var middleware = new LocalhostSecurityMiddleware(_next, NullLogger<LocalhostSecurityMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        await middleware.InvokeAsync(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [TestMethod(DisplayName = "InvokeAsync_IPv6Loopback_CallsNext")]
    public async Task InvokeAsync_IPv6Loopback_CallsNext()
    {
        var middleware = new LocalhostSecurityMiddleware(_next, NullLogger<LocalhostSecurityMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;

        await middleware.InvokeAsync(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [TestMethod(DisplayName = "InvokeAsync_ExternalIp_Returns403")]
    public async Task InvokeAsync_ExternalIp_Returns403()
    {
        var middleware = new LocalhostSecurityMiddleware(_next, NullLogger<LocalhostSecurityMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

        await middleware.InvokeAsync(context);

        Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [TestMethod(DisplayName = "InvokeAsync_NullIp_Returns403")]
    public async Task InvokeAsync_NullIp_Returns403()
    {
        var middleware = new LocalhostSecurityMiddleware(_next, NullLogger<LocalhostSecurityMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        await middleware.InvokeAsync(context);

        Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }
}
