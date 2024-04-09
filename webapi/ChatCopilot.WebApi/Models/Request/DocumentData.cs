namespace ChatCopilot.WebApi.Models.Request;

public sealed class DocumentData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("isUploaded")]
    public bool IsUploaded { get; set; } = false;
}
