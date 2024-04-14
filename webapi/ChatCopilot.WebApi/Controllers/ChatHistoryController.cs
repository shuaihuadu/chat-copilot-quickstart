namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ChatHistoryController : ControllerBase
{
    private const string ChatEditedClientCall = "ChatEdited";
    private const string ChatDeletedClientCall = "ChatDeleted";
    private const string GetChatRoute = "GetChatRoute";

    private readonly ILogger<ChatHistoryController> _logger;
    private readonly IKernelMemory _kernelMemory;
    private readonly IAuthInfo _authInfo;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly ChatMessageRepository _chatMessageRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository;
    private readonly ChatMemorySourceRepository _chatMemorySourceRepository;
    private readonly PromptsOptions _promptOptions;

    public ChatHistoryController(
        IOptions<PromptsOptions> promptsOptions,
        ILogger<ChatHistoryController> logger,
        IKernelMemory kernelMemory,
        IAuthInfo authInfo,
        ChatSessionRepository chatSessionRepository,
        ChatMessageRepository chatMessageRepository,
        ChatParticipantRepository chatParticipantRepository,
        ChatMemorySourceRepository chatMemorySourceRepository)
    {
        this._promptOptions = promptsOptions.Value;
        this._logger = logger;
        this._kernelMemory = kernelMemory;
        this._authInfo = authInfo;
        this._chatSessionRepository = chatSessionRepository;
        this._chatParticipantRepository = chatParticipantRepository;
        this._chatMessageRepository = chatMessageRepository;
        this._chatMemorySourceRepository = chatMemorySourceRepository;
    }

    [HttpPost]
    [Route("chats")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChatSessionAsync([FromBody] CreateChatParameters chatParameters)
    {
        if (chatParameters.Title == null)
        {
            return this.BadRequest("Chat session parameters cannot be null.");
        }

        ChatSession newChat = new ChatSession(chatParameters.Title, this._promptOptions.SystemDescription);

        await this._chatSessionRepository.CreateAsync(newChat);

        CopilotChatMessage chatMessage = CopilotChatMessage.CreateBotResponseMessage(
            newChat.Id,
            this._promptOptions.InitialBotMessage,
            string.Empty,
            null,
            TokenUtils.EmptyTokenUsage());

        await this._chatMessageRepository.CreateAsync(chatMessage);

        await this._chatParticipantRepository.CreateAsync(new ChatParticipant(this._authInfo.UserId, newChat.Id));

        this._logger.LogDebug("Created chat session with id {0}.", newChat.Id);

        return this.CreatedAtRoute(GetChatRoute, new { chatId = newChat.Id }, new CreateChatResponse(newChat, chatMessage));
    }

    [HttpGet]
    [Route("chats/{chatId:guid}", Name = GetChatRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatSessionByIdAsync(Guid chatId)
    {
        ChatSession? chat = null;

        if (await this._chatSessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            return this.Ok(chat);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    [HttpGet]
    [Route("chats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllChatSessionsAsync()
    {
        IEnumerable<ChatParticipant> chatParticipants = await this._chatParticipantRepository.FindByUserIdAsync(this._authInfo.UserId);

        List<ChatSession> chats = [];

        foreach (var chatParticipant in chatParticipants)
        {
            ChatSession? chat = null;

            if (await this._chatSessionRepository.TryFindByIdAsync(chatParticipant.ChatId, callback: v => chat = v))
            {
                chats.Add(chat!);
            }
            else
            {
                this._logger.LogDebug("Failed to find chat session with id {0}", chatParticipant.ChatId);
            }
        }

        return this.Ok(chats);
    }

    [HttpGet]
    [Route("chats/{chatId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatMessagesAsync(
        [FromRoute] Guid chatId,
        [FromQuery] int skip = 0,
        [FromQuery] int count = -1)
    {
        IEnumerable<CopilotChatMessage> chatMessages = await this._chatMessageRepository.FindByChatIdAsync(chatId.ToString(), skip, count);

        if (!chatMessages.Any())
        {
            return this.NotFound($"No messages found for chat id '{chatId}'.");
        }

        return this.Ok(chatMessages);
    }

    [HttpPatch]
    [Route("chats/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> EditChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] EditChatParameters chatParameters,
        [FromRoute] Guid chatId)
    {
        ChatSession? chat = null;

        if (await this._chatSessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            chat!.Title = chatParameters.Title ?? string.Empty;
            chat.SystemDescription = chatParameters.SystemDescription ?? chat.SafeSystemDescription;
            chat.MemoryBalance = chatParameters.MemoryBalance ?? chat.MemoryBalance;

            await this._chatSessionRepository.UpsertAsync(chat);
            await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync(ChatEditedClientCall, chat);

            return this.Ok(chat);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    [HttpGet]
    [Route("chats/{chatId:guid}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult<IEnumerable<MemorySource>>> GetSourcesAsync(Guid chatId)
    {
        this._logger.LogInformation("Get imported sources of chat session {0}", chatId);

        if (await this._chatSessionRepository.TryFindByIdAsync(chatId.ToString()))
        {
            IEnumerable<MemorySource> sources = await this._chatMemorySourceRepository.FindByChatIdAsync(chatId.ToString());

            return this.Ok(sources);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    [HttpDelete]
    [Route("chats/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> DeleteChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        Guid chatId,
        CancellationToken cancellationToken)
    {
        string chatIdString = chatId.ToString();

        ChatSession? chatToDelete = null;

        try
        {
            chatToDelete = await this._chatSessionRepository.FindByIdAsync(chatIdString);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"No chat session found for chat id '{chatId}'.");
        }

        try
        {
            await this.DeleteChatResourcesAsync(chatIdString, cancellationToken);
        }
        catch (AggregateException)
        {

            return this.StatusCode(StatusCodes.Status500InternalServerError, $"Faild to delete resources for chat id '{chatId}'.");
        }

        await this._chatSessionRepository.DeleteAsync(chatToDelete);

        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(ChatDeletedClientCall, chatIdString, this._authInfo.UserId, cancellationToken);

        return this.NoContent();
    }

    private async Task DeleteChatResourcesAsync(string chatId, CancellationToken cancellationToken)
    {
        List<Task> cleanupTasks = [];

        IEnumerable<ChatParticipant> chatParticipants = await this._chatParticipantRepository.FindByChatIdAsync(chatId);
        foreach (var participant in chatParticipants)
        {
            cleanupTasks.Add(this._chatParticipantRepository.DeleteAsync(participant));
        }

        IEnumerable<CopilotChatMessage> messages = await this._chatMessageRepository.FindByChatIdAsync(chatId);
        foreach (var message in messages)
        {
            cleanupTasks.Add(this._chatMessageRepository.DeleteAsync(message));
        }

        IEnumerable<MemorySource> sources = await this._chatMemorySourceRepository.FindByChatIdAsync(chatId, false);
        foreach (var source in sources)
        {
            cleanupTasks.Add(this._chatMemorySourceRepository.DeleteAsync(source));
        }

        cleanupTasks.Add(this._kernelMemory.RemoveChatMemoriesAsync(this._promptOptions.MemoryIndexName, chatId, cancellationToken));

        Task aggregationTask = Task.WhenAll(cleanupTasks);

        try
        {
            await aggregationTask;
        }
        catch (Exception ex)
        {
            if (aggregationTask?.Exception?.InnerException != null && aggregationTask.Exception.InnerExceptions.Count != 0)
            {
                foreach (var innerException in aggregationTask.Exception.InnerExceptions)
                {
                    this._logger.LogInformation("Faild to delete an entity of chat {0}:{1}", chatId, innerException.Message);
                }

                throw aggregationTask.Exception;
            }

            throw new AggregateException($"Resource deletion faild for chat '{chatId}'.", ex);
        }
    }
}
