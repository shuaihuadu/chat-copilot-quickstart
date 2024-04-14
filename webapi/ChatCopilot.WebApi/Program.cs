namespace ChatCopilot.WebApi;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.AddConfiguration();
        builder.WebHost.UseUrls();

        builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>())
            .AddOptions(builder.Configuration)
            .AddPersistentChatStore()
            .AddPlugins(builder.Configuration)
            .AddChatCopilotAuthentication(builder.Configuration)
            .AddChatCopilotAuthorization();

        builder.AddBotConfig()
            .AddSemanticKernelServices()
            .AddSemanticMemoryServices();

        builder.Services.AddSignalR();

        builder.Services.AddHttpContextAccessor()
            .AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            })
            .AddSingleton<ITelemetryInitializer, ApplicationInsightsUserTelemetryInitializerService>()
            .AddLogging(logBuilder => logBuilder.AddApplicationInsights())
            .AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();

        TelemetryDebugWriter.IsTracingDisabled = Debugger.IsAttached;

        builder.Services.AddHttpClient();

        builder.Services
            .AddMaintenanceServices()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddCorsPolicy(builder.Configuration)
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        builder.Services.AddHealthChecks();

        WebApplication app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<MaintenanceMiddleware>();
        app.MapControllers()
           .RequireAuthorization();
        app.MapHealthChecks("/healthz");

        app.MapHub<MessageRelayHub>("/messageRelayHub");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapWhen(
                context => context.Request.Path == "/",
                appBuilder => appBuilder.Run(
                    async context => await Task.Run(() => context.Response.Redirect("/swagger"))
                    )
                );
        }

        Task runTask = app.RunAsync();

        try
        {
            string? address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

            app.Services.GetRequiredService<ILogger>().LogInformation("Health probe: {0}/healthz", address);
        }
        catch (ObjectDisposedException)
        {
        }

        await runTask;
    }
}
