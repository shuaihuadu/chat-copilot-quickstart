namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ChatMemoryController : ControllerBase
{
    private readonly ILogger<ChatMemoryController> _logger;
    private readonly PromptsOptions _promptOptions;
    private readonly ChatSessionRepository _chatSessionRepository;

    public ChatMemoryController(
        ILogger<ChatMemoryController> logger,
        IOptions<PromptsOptions> promptOptions,
        ChatSessionRepository chatSessionRepository)
    {
        this._logger = logger;
        this._promptOptions = promptOptions.Value;
        this._chatSessionRepository = chatSessionRepository;
    }

    [HttpGet]
    [Route("chats/{chatId:guid}/memories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetSemanticMemoriesAsync(
        [FromServices] IKernelMemory kernelMemory,
        [FromRoute] string chatId,
        [FromQuery] string type)
    {
        string sanitizedChatId = GetSanitizedParameter(chatId);
        string sanitizedMemoryType = GetSanitizedParameter(type);

        if (!this._promptOptions.TryGetMemoryContainerName(type, out string memoryContainerName))
        {
            this._logger.LogWarning("Memory type: {0} is invalid.", sanitizedMemoryType);

            return this.BadRequest($"Memory type {sanitizedMemoryType} is invalid.");
        }

        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
        {
            this._logger.LogWarning("Chat session: {0} does not exists.", sanitizedChatId);

            return this.BadRequest($"Chat session: {sanitizedChatId} does not exists.");
        }

        List<string> memories = [];

        try
        {
            MemoryFilter filter = [];

            filter.ByTag("chatid", chatId);
            filter.ByTag("memory", memoryContainerName);

            SearchResult searchResult = await kernelMemory.SearchMemoryAsync(
                this._promptOptions.MemoryIndexName,
                "*",
                relevanceThreshold: 0,
                resultCount: 1,
                chatId,
                memoryContainerName);

            foreach (var memory in searchResult.Results.SelectMany(c => c.Partitions))
            {
                memories.Add(memory.Text);
            }

        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            this._logger.LogError(ex, "Cannot search collection {0}", memoryContainerName);
        }

        return this.Ok(memories);
    }

    private static string GetSanitizedParameter(string parameterValue)
    {
        return parameterValue.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal);
    }
}
