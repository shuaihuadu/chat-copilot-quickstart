namespace ChatCopilot.WebApi.Plugins.Chat;

public class SemanticChatMemoryItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("details")]
    public string Details { get; set; }

    public SemanticChatMemoryItem(string label, string details)
    {
        this.Label = label;
        this.Details = details;
    }

    public string ToFormattedString()
    {
        return $"{this.Label}: {this.Details?.Trim()}";
    }
}