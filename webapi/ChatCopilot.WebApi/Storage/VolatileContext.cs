namespace ChatCopilot.WebApi.Storage;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class VolatileContext<T> : IStorageContext<T> where T : IStorageEntity
{
    protected readonly ConcurrentDictionary<string, T> _entities;

    public VolatileContext()
    {
        this._entities = new ConcurrentDictionary<string, T>();
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return Task.FromResult(this._entities.Values.Where(predicate));
    }

    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty");
        }

        this._entities.TryAdd(entity.Id, entity);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty");
        }

        this._entities.TryRemove(entity.Id, out _);

        return Task.CompletedTask;
    }

    public Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty");
        }

        if (this._entities.TryGetValue(entityId, out T? entity))
        {
            return Task.FromResult(entity);
        }

        throw new KeyNotFoundException($"Entity with id {entityId} not found.");
    }

    public Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty");
        }

        this._entities.AddOrUpdate(entity.Id, entity, (key, oldValue) => entity);

        return Task.CompletedTask;
    }

    private string GetDebuggerDisplay()
    {
        return this.ToString() ?? string.Empty;
    }
}

public class VolatileCopilotChatMessageContext : VolatileContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1)
    {
        return Task.Run(
            () => this._entities.Values
            .Where(predicate).OrderByDescending(m => m.Timestamp).Skip(skip).Take(count));
    }
}