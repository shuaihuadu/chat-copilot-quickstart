namespace ChatCopilot.WebApi;

public sealed class Program
{
    public static void Main(string[] args)
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
            .AddApplicationInsightsTelemetry();

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}
