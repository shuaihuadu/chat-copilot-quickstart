namespace ChatCopilot.WebApi.Storage;

public interface IStorageEntity
{
    string Id { get; set; }

    string Partition { get; }
}