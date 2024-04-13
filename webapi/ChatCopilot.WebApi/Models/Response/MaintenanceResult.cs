namespace ChatCopilot.WebApi.Models.Response;

public class MaintenanceResult
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Note { get; set; }
}