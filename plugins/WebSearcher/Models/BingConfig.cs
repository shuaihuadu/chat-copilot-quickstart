namespace Plugins.WebSearcher.Models;

public sealed class BingConfig
{
    public const string SectionName = "Bing";

    public string BingApiBaseUrl => "https://api.bing.microsoft.com/v7.0/search";

    public string ApiKey { get; set; } = string.Empty;
}
