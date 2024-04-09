namespace ChatCopilot.WebApi.Storage;

public class FileSystemContext<T> : IStorageContext<T> where T : IStorageEntity
{
    protected readonly EntityDictionary _entities;
    private readonly FileInfo _fileStorage;
    private readonly object _fileStorageLock = new();


    public FileSystemContext(FileInfo filePath)
    {
        this._fileStorage = filePath;

        this._entities = this.Load(this._fileStorage);
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return Task.FromResult(this._entities.Values.Where(predicate));
    }

    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        if (this._entities.TryAdd(entity.Id, entity))
        {
            this.Save(this._entities, this._fileStorage);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        if (this._entities.TryRemove(entity.Id, out _))
        {
            this.Save(this._entities, this._fileStorage);
        }

        return Task.CompletedTask;
    }

    public Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        if (this._entities.TryGetValue(entityId, out T? entity))
        {
            return Task.FromResult(entity);
        }

        return Task.FromException<T>(new KeyNotFoundException($"Entity with id {entityId} not found."));
    }

    public Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        if (this._entities.AddOrUpdate(entity.Id, entity, (key, oldValue) => entity) != null)
        {
            this.Save(this._entities, this._fileStorage);
        }

        return Task.CompletedTask;
    }

    private void Save(EntityDictionary entities, FileInfo fileInfo)
    {
        lock (this._fileStorageLock)
        {
            if (!fileInfo.Exists)
            {
                fileInfo.Directory!.Create();

                File.WriteAllText(fileInfo.FullName, "{}");
            }

            using FileStream fileStream = File.Open(
                path: fileInfo.FullName,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Write,
                share: FileShare.Read);

            JsonSerializer.Serialize(fileStream, entities);
        }
    }

    private EntityDictionary Load(FileInfo fileInfo)
    {
        lock (this._fileStorageLock)
        {
            if (!fileInfo.Exists)
            {
                fileInfo.Directory!.Create();

                File.WriteAllText(fileInfo.FullName, "{}");
            }

            using FileStream fileStream = File.Open(
                path: fileInfo.FullName,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Read,
                share: FileShare.Read);

            return JsonSerializer.Deserialize<EntityDictionary>(fileStream) ?? new EntityDictionary();
        }
    }

    protected sealed class EntityDictionary : ConcurrentDictionary<string, T> { }
}

public class FileSystemCopilotChatMessageContext : FileSystemContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    public FileSystemCopilotChatMessageContext(FileInfo filePath) : base(filePath) { }

    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1)
    {
        return Task.Run(
            () => this._entities.Values
            .Where(predicate).OrderByDescending(m => m.Timestamp).Skip(skip).Take(count));
    }
}