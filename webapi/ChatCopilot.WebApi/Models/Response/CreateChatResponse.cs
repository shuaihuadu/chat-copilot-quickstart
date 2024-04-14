namespace ChatCopilot.WebApi.Models.Response;

public class CreateChatResponse
{
    [JsonPropertyName("chatSession")]
    public ChatSession ChatSession { get; set; }

    [JsonPropertyName("initialBotMessage")]
    public CopilotChatMessage InitialBotMessage { get; set; }

    public CreateChatResponse(ChatSession chatSession, CopilotChatMessage initialBotMessage)
    {
        this.ChatSession = chatSession;
        this.InitialBotMessage = initialBotMessage;
    }
}