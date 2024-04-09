namespace ChatCopilot.WebApi.Models.Response;

public class ChatArchiveEmbeddingConfig
{
    public enum AIServiceType
    {
        AzureOpenAIEmbedding,
        OpenAI
    }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AIServiceType AIService { get; set; } = AIServiceType.AzureOpenAIEmbedding;

    public string DeploymentOrModelId { get; set; } = string.Empty;
}
