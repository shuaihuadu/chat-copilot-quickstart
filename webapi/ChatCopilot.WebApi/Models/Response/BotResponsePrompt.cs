namespace ChatCopilot.WebApi.Models.Response;

public class BotResponsePrompt
{
    [JsonPropertyName("systemPersona")]
    public string SystemPersona { get; set; } = string.Empty;

    [JsonPropertyName("audience")]
    public string Audience { get; set; } = string.Empty;

    [JsonPropertyName("userIntent")]
    public string UserIntent { get; set; } = string.Empty;

    [JsonPropertyName("chatMemories")]
    public string PastMemories { get; set; } = string.Empty;

    [JsonPropertyName("chatHistory")]
    public string ChatHistory { get; set; } = string.Empty;

    [JsonPropertyName("metaPromptTemplate")]
    public ChatHistory MetaPromptTemplate { get; set; } = [];

    public BotResponsePrompt(
        string systemInstructions,
        string audience,
        string userIntent,
        string chatMemories,
        string chatHistory,
        ChatHistory metaPromptTemplate)
    {
        this.SystemPersona = systemInstructions;
        this.Audience = audience;
        this.UserIntent = userIntent;
        this.PastMemories = chatMemories;
        this.ChatHistory = chatHistory;
        this.MetaPromptTemplate = metaPromptTemplate;
    }
}