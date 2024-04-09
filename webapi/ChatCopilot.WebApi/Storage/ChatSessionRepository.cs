namespace ChatCopilot.WebApi.Storage;

public class ChatSessionRepository : Repository<ChatSession>
{
    public ChatSessionRepository(IStorageContext<ChatSession> storageContext) : base(storageContext)
    {
    }

    public Task<IEnumerable<ChatSession>> GetAllChatsAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }
}
