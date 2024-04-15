namespace ChatCopilot.WebApi.Models.Request;

public class DocumentImportForm
{
    public IEnumerable<IFormFile> FormFiles { get; set; } = [];

    public bool UseContentSafety { get; set; } = false;
}