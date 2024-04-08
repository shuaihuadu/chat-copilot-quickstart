namespace ChatCopilot.WebApi.Options;

public class FileSystemOptions
{
    [Required, NotEmptyOrWhitespace]
    public string FilePath { get; set; } = string.Empty;

}