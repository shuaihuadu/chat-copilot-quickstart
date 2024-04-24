namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ChatArchiveController(
    ILogger<ChatArchiveController> logger,
    IKernelMemory kernelMemory,
    IOptions<PromptsOptions> promptsOptions,
    ChatSessionRepository chatSessionRepository,
    ChatMessageRepository chatMessageRepository,
    ChatParticipantRepository chatParticipantRepository,
    ChatArchiveEmbeddingConfig chatArchiveEmbeddingConfig) : ControllerBase
{
    private readonly ILogger<ChatArchiveController> _logger = logger;
    private readonly IKernelMemory _kernelMemory = kernelMemory;
    private readonly ChatSessionRepository _chatSessionRepository = chatSessionRepository;
    private readonly ChatMessageRepository _chatMessageRepository = chatMessageRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository = chatParticipantRepository;
    private readonly ChatArchiveEmbeddingConfig _chatArchiveEmbeddingConfig = chatArchiveEmbeddingConfig;
    private readonly PromptsOptions _promptsOptions = promptsOptions.Value;

    [HttpGet]
    [Route("chats/{chatId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult<ChatArchive?>> DownloadAsync(Guid chatId, CancellationToken cancellationToken)
    {
        this._logger.LogDebug("Received call to download a chat archive");

        ChatArchive chatArchive = await this.CreateChatArchiveAsync(chatId, cancellationToken);

        return this.Ok(chatArchive);
    }

    private async Task<ChatArchive> CreateChatArchiveAsync(Guid chatId, CancellationToken cancellationToken)
    {
        string chatIdString = chatId.ToString();

        ChatArchive chatArchive = new ChatArchive
        {
            EmbeddingConfigurations = this._chatArchiveEmbeddingConfig
        };

        ChatSession chat = await this._chatSessionRepository.FindByIdAsync(chatIdString);

        chatArchive.ChatTitle = chat.Title;

        chatArchive.SystemDescription = chat.SafeSystemDescription;

        chatArchive.ChatHistory = await this.GetAllChatMessagesAsync(chatIdString);

        foreach (var memory in this._promptsOptions.MemoryMap.Keys)
        {
            chatArchive.Embeddings.Add(memory, await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(chatIdString, memory, cancellationToken));
        }

        chatArchive.DocumentEmbeddings.Add("GlobalDocuments", await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(Guid.Empty.ToString(), this._promptsOptions.DocumentMemoryName, cancellationToken));

        chatArchive.DocumentEmbeddings.Add("ChatDocuments", await this.GetMemoryRecordsAndAppendToEmbeddingsAsync(chatIdString, this._promptsOptions.DocumentMemoryName, cancellationToken));

        return chatArchive;
    }

    private async Task<List<Citation>> GetMemoryRecordsAndAppendToEmbeddingsAsync(string chatId, string memoryName, CancellationToken cancellationToken)
    {
        List<Citation> collectionMemoryRecords;

        try
        {
            SearchResult result = await this._kernelMemory.SearchMemoryAsync(
                this._promptsOptions.MemoryIndexName,
                query: "*",
                relevanceThreshold: -1,
                chatId,
                memoryName,
                cancellationToken);

            collectionMemoryRecords = result.Results;
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            this._logger.LogError(ex, "Cannot search collection {0}", memoryName);

            collectionMemoryRecords = [];
        }

        return collectionMemoryRecords;
    }

    private async Task<List<CopilotChatMessage>> GetAllChatMessagesAsync(string chatId)
    {
        return (await this._chatMessageRepository.FindByChatIdAsync(chatId)).ToList();
    }
}
