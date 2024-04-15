using System.Text.Json.Serialization;

namespace Plugins.WebSearcher.Models;

internal sealed class WebPage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;
}

internal sealed class WebPages
{
    [JsonPropertyName("value")]
    public WebPage[]? Value { get; set; }
}

internal sealed class BingSearchResponse
{
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
}
