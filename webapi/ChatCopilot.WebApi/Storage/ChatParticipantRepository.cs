namespace ChatCopilot.WebApi.Storage;

public class ChatParticipantRepository : Repository<ChatParticipant>
{
    public ChatParticipantRepository(IStorageContext<ChatParticipant> storageContext) : base(storageContext)
    {
    }

    public Task<IEnumerable<ChatParticipant>> FindByUserIdAsync(string userId)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId);
    }

    public Task<IEnumerable<ChatParticipant>> FindByChatIdAsync(string chatId)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.ChatId == chatId);
    }

    public async Task<bool> IsUserInChatAsync(string userId, string chatId)
    {
        IEnumerable<ChatParticipant> users = await base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId && e.ChatId == chatId);

        return users.Any();
    }
}
