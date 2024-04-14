namespace ChatCopilot.WebApi.Models.Request;

public class CreateChatParameters
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}