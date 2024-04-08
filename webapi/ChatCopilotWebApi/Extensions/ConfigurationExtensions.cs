namespace ChatCopilot.WebApi.Extensions;

internal static class ConfigurationExtensions
{
    public static IHostBuilder AddConfiguration(this IHostBuilder host)
    {
        string? envrionment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        host.ConfigureAppConfiguration((builderContext, configBuilder) =>
        {
            configBuilder.AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true);

            configBuilder.AddJsonFile(path: $"appsettiongs.{envrionment}.json", true, true);

            configBuilder.AddEnvironmentVariables();

            configBuilder.AddUserSecrets(assembly: Assembly.GetExecutingAssembly(), optional: true, reloadOnChange: true);

            string? keyVaultUri = builderContext.Configuration["Service:KeyVault"];

            if (!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                configBuilder.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
            }
        });

        return host;
    }
}
