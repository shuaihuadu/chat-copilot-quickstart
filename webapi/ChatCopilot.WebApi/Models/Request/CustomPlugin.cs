namespace ChatCopilot.WebApi.Models.Request;

public class CustomPlugin
{
    [JsonPropertyName("nameForHuman")]
    public string NameForHuman { get; set; } = string.Empty;

    [JsonPropertyName("nameForModel")]
    public string NameForModel { get; set; } = string.Empty;

    [JsonPropertyName("authHeaderTag")]
    public string AuthHeaderTag { get; set; } = string.Empty;

    [JsonPropertyName("authType")]
    public string AuthType { get; set; } = string.Empty;

    [JsonPropertyName("manifestDomain")]
    public string ManifestDomain { get; set; } = string.Empty;
}