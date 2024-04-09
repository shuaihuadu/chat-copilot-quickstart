namespace ChatCopilot.WebApi.Storage;

public interface IRepository<T> where T : IStorageEntity
{
    Task CreateAsync(T entity);

    Task DeleteAsync(T entity);

    Task UpsertAsync(T entity);

    Task<T> FindByIdAsync(string id, string partition);

    Task<bool> TryFindByIdAsync(string id, string partition, Action<T?> callback);
}
