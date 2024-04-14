namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ChatParticipantController : ControllerBase
{
    private const string UserJoinedClientCall = "UserJoined";

    private readonly ILogger<ChatParticipantController> _logger;
    private readonly ChatParticipantRepository _chatParticipantRepository;
    private readonly ChatSessionRepository _chatSessionRepository;

    public ChatParticipantController(
        ILogger<ChatParticipantController> logger,
        ChatParticipantRepository chatParticipantRepository,
        ChatSessionRepository chatSessionRepository)
    {
        this._logger = logger;
        this._chatParticipantRepository = chatParticipantRepository;
        this._chatSessionRepository = chatSessionRepository;
    }

    [HttpPost]
    [Route("chats/{chatId:guid}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinChatAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromServices] IAuthInfo authInfo,
        [FromRoute] Guid chatId)
    {
        string userId = authInfo.UserId;

        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId.ToString()))
        {
            return this.BadRequest("Chat session does not exists.");
        }

        if (await this._chatParticipantRepository.IsUserInChatAsync(userId, chatId.ToString()))
        {
            return this.Conflict("User is already in the chat.");
        }

        ChatParticipant chatParticipant = new ChatParticipant(userId, chatId.ToString());
        await this._chatParticipantRepository.CreateAsync(chatParticipant);

        await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync(UserJoinedClientCall, chatId, userId);

        return this.Ok(chatParticipant);
    }

    [HttpGet]
    [Route("chats/{chatId:guid}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetAllParticipantsAsync(Guid chatId)
    {
        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId.ToString()))
        {
            return this.NotFound("Chat session does not exists.");
        }

        IEnumerable<ChatParticipant> chatParticipants = await this._chatParticipantRepository.FindByChatIdAsync(chatId.ToString());

        return Ok(chatParticipants);
    }
}
