namespace ChatCopilot.WebApi.Storage;

public interface IStorageContext<T> where T : IStorageEntity
{
    Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate);

    Task<T> ReadAsync(string entityId, string partitionKey);

    Task CreateAsync(T entity);

    Task UpsertAsync(T entity);

    Task DeleteAsync(T entity);
}

public interface ICopilotChatMessageStorageContext : IStorageContext<CopilotChatMessage>
{
    Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1);
}