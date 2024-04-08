namespace ChatCopilot.WebApi.Options;

public class ChatStoreOptions
{
    public const string PropertyName = "ChatStore";

    public enum ChatStoreType
    {
        Volatile,
        FileSystem,
        Cosmos
    }

    public ChatStoreType Type { get; set; } = ChatStoreType.Volatile;

    [RequiredOnPropertyValue(nameof(Type), ChatStoreType.FileSystem)]
    public FileSystemOptions? FileSystem { get; set; }

    [RequiredOnPropertyValue(nameof(Type), ChatStoreType.Cosmos)]
    public CosmosOptions? Cosmos { get; set; }
}