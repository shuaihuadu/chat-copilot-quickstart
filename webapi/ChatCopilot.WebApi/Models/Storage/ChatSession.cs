namespace ChatCopilot.WebApi.Models.Storage;

public class ChatSession : IStorageEntity
{
    private const string CurrentVersion = "2.0";

    public string Id { get; set; }

    public string Title { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public string SystemDescription { get; set; }

    public string SafeSystemDescription => this.SystemDescription.Replace("TimeSkill", "TimePlugin", StringComparison.OrdinalIgnoreCase);

    public float MemoryBalance { get; set; } = 0.5F;

    public HashSet<string> EnabledPlugins { get; set; } = [];

    public string? Version { get; set; }

    [JsonIgnore]
    public string Partition => this.Id;

    public ChatSession(string title, string systemDescription)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Title = title;
        this.CreatedOn = DateTimeOffset.UtcNow;
        this.SystemDescription = systemDescription;
        this.Version = CurrentVersion;
    }
}
