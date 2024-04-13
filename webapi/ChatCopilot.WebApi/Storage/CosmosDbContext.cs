using Container = Microsoft.Azure.Cosmos.Container;

namespace ChatCopilot.WebApi.Storage;

public class CosmosDbContext<T> : IStorageContext<T>, IDisposable where T : IStorageEntity
{
    private readonly CosmosClient _client;

    protected readonly Container _container;

    public CosmosDbContext(string connectionString, string database, string container)
    {
        CosmosClientOptions options = new()
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        this._client = new CosmosClient(connectionString, options);

        this._container = this._client.GetContainer(database, container);
    }

    public async Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return await Task.Run(() => this._container.GetItemLinqQueryable<T>(true).Where(predicate).AsEnumerable());
    }

    public async Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }
        await this._container.CreateItemAsync(entity, new PartitionKey(entity.Partition));
    }

    public async Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        await this._container.DeleteItemAsync<T>(entity.Id, new PartitionKey(entity.Partition));
    }

    public async Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        try
        {
            ItemResponse<T> response = await this._container.ReadItemAsync<T>(entityId, new PartitionKey(partitionKey));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Entity with id {entityId} not found.");
        }
    }

    public async Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        await this._container.UpsertItemAsync(entity, new PartitionKey(entity.Partition));
    }

    public void Dispose()
    {
        this.Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._client.Dispose();
        }
    }
}

public class CosmosDbCopilotChatMessageContext : CosmosDbContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    public CosmosDbCopilotChatMessageContext(string connectionString, string database, string container) : base(connectionString, database, container)
    {
    }

    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1)
    {
        return Task.Run(() => this._container.GetItemLinqQueryable<CopilotChatMessage>(true)
        .Where(predicate).OrderByDescending(m => m.Timestamp).Skip(skip).Take(count).AsEnumerable());
    }
}