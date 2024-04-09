namespace ChatCopilot.WebApi.Storage;

public class ChatMemorySourceRepository : Repository<MemorySource>
{
    public ChatMemorySourceRepository(IStorageContext<MemorySource> storageContext) : base(storageContext)
    {
    }

    public Task<IEnumerable<MemorySource>> FindByChatIdAsync(string chatId, bool includeGlobal = true)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.ChatId == chatId || (includeGlobal && e.ChatId == Guid.Empty.ToString()));
    }

    public Task<IEnumerable<MemorySource>> FindByNameAsync(string name)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IEnumerable<MemorySource>> GetAllAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }
}
