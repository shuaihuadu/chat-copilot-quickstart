namespace ChatCopilot.WebApi;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.AddConfiguration();
        builder.WebHost.UseUrls();

        builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>())
            .AddOptions(builder.Configuration);

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}
