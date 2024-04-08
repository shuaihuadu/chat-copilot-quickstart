namespace ChatCopilot.WebApi.Options;

public class DocumentMemoryOptions
{
    public const string PropertyName = "DocumentMemory";

    internal static readonly Guid GlobalDocumentChatId = Guid.Empty;

    [Range(0, int.MaxValue)]
    public int DocumentLineSplitMaxTokens { get; set; } = 30;

    [Range(0, int.MaxValue)]
    public int DocumentChunkMaxTokens { get; set; } = 100;

    [Range(0, int.MaxValue)]
    public int FileSizeLimit { get; set; } = 1000000;

    [Range(0, int.MaxValue)]
    public int FileCountLimit { get; set; } = 10;
}
