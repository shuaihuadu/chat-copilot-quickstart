namespace ChatCopilot.WebApi.Models.Storage;

public class CitationSource
{
    public string Link { get; set; } = string.Empty;

    public string SourceContentType { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public double RelevanceScore { get; set; } = 0.0;

    public static CitationSource FromSemanticMemoryCitation(Citation citation, string snippet, double relevanceScore)
    {
        CitationSource citationSource = new()
        {
            Link = citation.Link,
            SourceContentType = citation.SourceContentType,
            SourceName = citation.SourceName,
            Snippet = snippet,
            RelevanceScore = relevanceScore
        };

        return citationSource;
    }
}
