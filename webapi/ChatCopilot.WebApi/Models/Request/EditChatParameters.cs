namespace ChatCopilot.WebApi.Models.Request;

public class EditChatParameters
{
    public string? Title { get; set; }

    public string? SystemDescription { get; set; }

    public float? MemoryBalance { get; set; }
}