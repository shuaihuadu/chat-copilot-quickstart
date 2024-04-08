namespace ChatCopilot.WebApi.Options;

public class CosmosOptions
{
    [Required, NotEmptyOrWhitespace]
    public string Database { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ConnectionString { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ChatSessionsContainer { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ChatMessagesContainer { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ChatMemorySourcesContainer { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string ChatParticipantsContainer { get; set; } = string.Empty;

}