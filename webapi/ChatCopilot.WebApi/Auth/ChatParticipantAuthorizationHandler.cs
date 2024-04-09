namespace ChatCopilot.WebApi.Auth;

internal class ChatParticipantAuthorizationHandler : AuthorizationHandler<ChatParticipantRequirement, HttpContext>
{
    public readonly IAuthInfo _authInfo;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository;

    public ChatParticipantAuthorizationHandler(
        IAuthInfo authInfo,
        ChatSessionRepository chatSessionRepository,
        ChatParticipantRepository chatParticipantRepository)
    {
        this._authInfo = authInfo;
        this._chatSessionRepository = chatSessionRepository;
        this._chatParticipantRepository = chatParticipantRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ChatParticipantRequirement requirement, HttpContext resource)
    {
        try
        {
            string? chatId = resource.GetRouteValue("chatId")?.ToString();

            if (chatId == null)
            {
                context.Succeed(requirement);
                return;
            }

            ChatSession session = await this._chatSessionRepository.FindByIdAsync(chatId);

            if (session == null)
            {
                context.Succeed(requirement);
                return;
            }

            bool isUserInChat = await this._chatParticipantRepository.IsUserInChatAsync(this._authInfo.UserId, chatId);

            if (!isUserInChat)
            {
                context.Fail(new AuthorizationFailureReason(this, "User does not have access to the requested chat."));
            }

            context.Succeed(requirement);
        }
        catch (CredentialUnavailableException ex)
        {
            context.Fail(new AuthorizationFailureReason(this, ex.Message));
        }
    }
}