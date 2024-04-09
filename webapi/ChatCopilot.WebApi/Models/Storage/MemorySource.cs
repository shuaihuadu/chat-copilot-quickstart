namespace ChatCopilot.WebApi.Models.Storage;

public class MemorySource : IStorageEntity
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("chatId")]
    public string ChatId { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("sourceType")]
    public MemorySourceType SourceType { get; set; } = MemorySourceType.File;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("hyperlink")]
    public Uri? HyperLink { get; set; } = null;

    [JsonPropertyName("sharedBy")]
    public string SharedBy { get; set; } = string.Empty;

    [JsonPropertyName("createOn")]
    public DateTimeOffset CreateOn { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("tokens")]
    public long Tokens { get; set; } = 0;

    [JsonIgnore]
    public string Partition => this.ChatId;

    public MemorySource() { }

    public MemorySource(string chatId, string name, string sharedBy, MemorySourceType type, long size, Uri? hyperlink)
    {
        this.Id = Guid.NewGuid().ToString();
        this.ChatId = chatId;
        this.Name = name;
        this.SourceType = type;
        this.HyperLink = hyperlink;
        this.SharedBy = sharedBy;
        this.CreateOn = DateTimeOffset.UtcNow;
        this.Size = size;
    }
}

public enum MemorySourceType
{
    File
}