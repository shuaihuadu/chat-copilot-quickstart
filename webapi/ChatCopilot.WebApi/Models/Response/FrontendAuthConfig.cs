namespace ChatCopilot.WebApi.Models.Response;

public class FrontendAuthConfig
{
    [JsonPropertyName("authType")]
    public string AuthType { get; set; } = string.Empty;

    [JsonPropertyName("aadAuthority")]
    public string AadAuthority { get; set; } = string.Empty;

    [JsonPropertyName("aadClientId")]
    public string AadClientId { get; set; } = string.Empty;

    [JsonPropertyName("aadApiScope")]
    public string AadApiScope { get; set; } = string.Empty;
}