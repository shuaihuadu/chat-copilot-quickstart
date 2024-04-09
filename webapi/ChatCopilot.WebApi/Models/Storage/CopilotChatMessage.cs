namespace ChatCopilot.WebApi.Models.Storage;

public class CopilotChatMessage : IStorageEntity
{
    private static readonly JsonSerializerOptions SerializerSettings = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public enum AuthorRoles
    {
        User = 0,
        Bot
    }

    public enum ChatMessageType
    {
        Message,
        Plan,
        Document
    }

    public DateTimeOffset Timestamp { get; set; }

    public string UserId { get; set; }

    public string UserName { get; set; }

    public string ChatId { get; set; }

    public string Content { get; set; }

    public string Id { get; set; }

    public AuthorRoles AuthorRole { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public IEnumerable<CitationSource>? Citations { get; set; }

    public ChatMessageType Type { get; set; }

    public IDictionary<string, int>? TokenUsage { get; set; }

    [JsonIgnore]
    public string Partition => this.ChatId;

    public CopilotChatMessage(
        string userId,
        string userName,
        string chatId,
        string content,
        string? prompt = null,
        IEnumerable<CitationSource>? citations = null,
        AuthorRoles authorRole = AuthorRoles.User,
        ChatMessageType type = ChatMessageType.Message,
        IDictionary<string, int>? tokenUsage = null)
    {
        this.Timestamp = DateTimeOffset.UtcNow;
        this.UserId = userId;
        this.UserName = userName;
        this.ChatId = chatId;
        this.Content = content;
        this.Id = Guid.NewGuid().ToString();
        this.Prompt = prompt ?? string.Empty;
        this.Citations = citations;
        this.AuthorRole = authorRole;
        this.Type = type;
        this.TokenUsage = tokenUsage;
    }

    public static CopilotChatMessage CreateBotResponseMessage(
        string chatId,
        string content,
        string prompt,
        IEnumerable<CitationSource>? citations,
        IDictionary<string, int>? tokenUsage = null)
    {
        return new CopilotChatMessage("Bot", "Bot", chatId, content, prompt, citations, AuthorRoles.Bot, ChatMessageType.Message, tokenUsage);
    }

    public static CopilotChatMessage CreateDocumentMessage(string userId, string userName, string chatId, DocumentMessageContent documentMessageContent)
    {
        return new CopilotChatMessage(userId, userName, chatId, documentMessageContent.ToString(), string.Empty, null, AuthorRoles.User, ChatMessageType.Document);
    }

    public string ToFormattedString()
    {
        string messagePrefix = $"[{this.Timestamp.ToString("G", CultureInfo.CurrentCulture)}]";

        switch (this.Type)
        {
            case ChatMessageType.Plan:
            case ChatMessageType.Message:
                return $"{messagePrefix} {this.UserName} said: {this.Content}";

            case ChatMessageType.Document:
                DocumentMessageContent? documentMessage = DocumentMessageContent.FromString(this.Content);
                string documentMessageContent = (documentMessage != null) ? documentMessage.ToFormattedString() : "documents";

                return $"{messagePrefix} {this.UserName} uploaded: {documentMessageContent}";
            default:
                throw new InvalidOperationException($"Unknown message type: {this.Type}");
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, SerializerSettings);
    }

    public static CopilotChatMessage? FromString(string json)
    {
        return JsonSerializer.Deserialize<CopilotChatMessage>(json, SerializerSettings);
    }
}
