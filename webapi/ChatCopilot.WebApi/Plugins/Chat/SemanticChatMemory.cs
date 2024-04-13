namespace ChatCopilot.WebApi.Plugins.Chat;

public class SemanticChatMemory
{
    [JsonPropertyName("items")]
    public List<SemanticChatMemoryItem> Items { get; set; } = [];

    public void AddItem(string label, string details)
    {
        this.Items.Add(new SemanticChatMemoryItem(label, details));
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public static SemanticChatMemory FromJson(string json)
    {
        SemanticChatMemory? result = JsonSerializer.Deserialize<SemanticChatMemory>(json);

        return result ?? throw new ArgumentException("Failed to deserialize chat memory to json.");
    }
}