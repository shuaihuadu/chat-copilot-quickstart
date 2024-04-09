namespace ChatCopilot.WebApi.Models.Response;

public class DocumentMessageContent
{
    [JsonPropertyName("documents")]
    public IEnumerable<DocumentData> Documents { get; set; } = [];

    public void AddDocument(string name, string size, bool isUploaded)
    {
        this.Documents = this.Documents.Append(new DocumentData
        {
            Name = name,
            Size = size,
            IsUploaded = isUploaded
        });
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public string ToFormattedString()
    {
        if (!this.Documents.Any())
        {
            return string.Empty;
        }

        List<string> formattedStrings = this.Documents
            .Where(document => document.IsUploaded)
            .Select(document => document.Name)
            .ToList();

        if (formattedStrings.Count == 1)
        {
            return formattedStrings.First();
        }

        return string.Join(", ", formattedStrings);
    }

    public string ToFormattedStringNamesOnly()
    {
        if (!this.Documents.Any())
        {
            return string.Empty;
        }

        List<string> formattedStrings = this.Documents
            .Where(document => document.IsUploaded)
            .Select(document => document.Name)
            .ToList();

        if (formattedStrings.Count == 1)
        {
            return formattedStrings.First();
        }

        return string.Join(", ", formattedStrings);
    }

    public static DocumentMessageContent? FromString(string json)
    {
        return JsonSerializer.Deserialize<DocumentMessageContent>(json);
    }
}
