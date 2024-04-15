using ChatCopilot.Shared;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

IKernelMemory kernelMemory = new KernelMemoryBuilder()
    .FromAppSettings()
    .WithCustomOcr(builder.Configuration)
    .Build();

builder.Services.AddSingleton(kernelMemory);

builder.Services.AddApplicationInsightsTelemetry();

WebApplication app = builder.Build();

DateTimeOffset start = DateTimeOffset.UtcNow;

app.MapGet("/", () =>
{
    long uptime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start.ToUnixTimeSeconds();

    string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    string message = $"Memory pipeline is running. Uptime: {uptime} secs.";

    if (!string.IsNullOrEmpty(environment))
    {
        message += $" Environment: {environment}";
    }

    return Results.Ok(message);
});

app.Logger.LogInformation(
    "Starting Chat Copilot Memory pipeline service, .NET Env: {0}, Log Level: {1}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName());

app.Run();