using Microsoft.Extensions.Configuration;

namespace ImportDocument;

public sealed class Config
{
    public string AuthenticationType { get; set; } = "None";

    public string ClientId { get; set; } = string.Empty;

    public string BackendClientId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string Instance { get; set; } = string.Empty;

    public string Scopes { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string ServiceUri { get; set; } = string.Empty;

    public static Config? GetConfig()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        return config.GetRequiredSection("Config").Get<Config>();
    }

    public static bool Validate(Config? config)
    {
        return config != null;
    }
}
