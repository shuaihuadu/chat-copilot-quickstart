namespace ChatCopilot.WebApi.Storage;

public class Repository<T> : IRepository<T> where T : IStorageEntity
{
    protected IStorageContext<T> StorageContext { get; set; }

    public Repository(IStorageContext<T> storageContext)
    {
        this.StorageContext = storageContext;
    }

    public Task CreateAsync(T entity)
    {
        return this.StorageContext.CreateAsync(entity);
    }

    public Task DeleteAsync(T entity)
    {
        return this.StorageContext.DeleteAsync(entity);
    }

    public Task<T> FindByIdAsync(string id, string? partition = null)
    {
        return this.StorageContext.ReadAsync(id, partition ?? id);
    }

    public async Task<bool> TryFindByIdAsync(string id, string? partition = null, Action<T?>? callback = null)
    {
        try
        {
            T? found = await this.FindByIdAsync(id, partition);

            callback?.Invoke(found);

            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is KeyNotFoundException)
        {
            return false;
        }
    }

    public Task UpsertAsync(T entity)
    {
        return this.StorageContext.UpsertAsync(entity);
    }
}

public class CopilotChatMessageRepository : Repository<CopilotChatMessage>
{
    private readonly ICopilotChatMessageStorageContext _storageContext;

    public CopilotChatMessageRepository(ICopilotChatMessageStorageContext storageContext) : base(storageContext)
    {
        this._storageContext = storageContext;
    }

    public async Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1)
    {
        return await Task.Run(() => this._storageContext.QueryEntitiesAsync(predicate, skip, count));
    }
}