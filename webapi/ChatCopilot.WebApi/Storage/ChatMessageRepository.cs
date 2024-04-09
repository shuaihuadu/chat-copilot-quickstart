namespace ChatCopilot.WebApi.Storage;

public class ChatMessageRepository : CopilotChatMessageRepository
{
    public ChatMessageRepository(ICopilotChatMessageStorageContext storageContext) : base(storageContext)
    {
    }

    public Task<IEnumerable<CopilotChatMessage>> FindByChatIdAsync(string chatId, int skip = 0, int count = -1)
    {
        return base.QueryEntitiesAsync(e => e.ChatId == chatId, skip, count);
    }

    public async Task<CopilotChatMessage> FindLastByChatIdAsync(string chatId)
    {
        IEnumerable<CopilotChatMessage> chatMessages = await this.FindByChatIdAsync(chatId, 0, 1);

        CopilotChatMessage? first = chatMessages.MaxBy(e => e.Timestamp);

        return first ?? throw new KeyNotFoundException($"No messages found for chat '{chatId}'.");
    }
}
