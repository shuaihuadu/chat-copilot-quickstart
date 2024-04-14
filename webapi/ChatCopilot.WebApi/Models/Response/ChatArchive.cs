namespace ChatCopilot.WebApi.Models.Response;

public class ChatArchive
{
    public ChatArchiveSchemaInfo Schema { get; set; } = new();

    public ChatArchiveEmbeddingConfig EmbeddingConfigurations { get; set; } = new();

    public string ChatTitle { get; set; } = string.Empty;

    public string SystemDescription { get; set; } = string.Empty;

    public List<CopilotChatMessage> ChatHistory { get; set; } = [];

    public Dictionary<string, List<Citation>> Embeddings { get; set; } = [];

    public Dictionary<string, List<Citation>> DocumentEmbeddings { get; set; } = [];
}