namespace ChatCopilot.WebApi.Options;

public record ChatArchiveSchemaInfo
{
    [Required, NotEmptyOrWhitespace]
    public string Name { get; init; } = "ChatCopilot";


    [Range(0, int.MaxValue)]
    public int Version { get; set; } = 1;
}
