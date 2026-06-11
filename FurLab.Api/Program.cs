using FurLab.Api.Middleware;
using FurLab.Api.Services;
using FurLab.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(options =>
{
    options.ListenLocalhost(5000);
});

builder.Services.AddFurLabServices();
builder.Services.AddSingleton<ExecutionRegistryService>();
builder.Services.AddSingleton<SseBroadcasterService>();
builder.Services.AddSingleton<StatusTrackerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseMiddleware<LocalhostSecurityMiddleware>();
app.MapControllers();

app.Run();
