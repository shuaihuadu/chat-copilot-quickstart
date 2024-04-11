namespace ChatCopilot.WebApi.Hubs;

public class MessageRelayHub : Hub
{
    private const string ReceiveMessageClientCall = "ReceiveMessage";
    private const string ReceiveUserTypingStateClientCall = "ReceiveUserTypingState";

    private readonly ILogger<MessageRelayHub> _logger;

    public MessageRelayHub(ILogger<MessageRelayHub> logger)
    {
        this._logger = logger;
    }

    public async Task AddClientToGroupAsync(string chatId)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, chatId);
    }

    public async Task SendMessageAsync(string chatId, string senderId, object message)
    {
        await this.Clients.OthersInGroup(chatId).SendAsync(ReceiveMessageClientCall, chatId, senderId, message);
    }

    public async Task SendUserTypingStateAsync(string chatId, string userId, bool isTyping)
    {
        await this.Clients.OthersInGroup(chatId).SendAsync(ReceiveUserTypingStateClientCall, chatId, userId, isTyping);
    }
}