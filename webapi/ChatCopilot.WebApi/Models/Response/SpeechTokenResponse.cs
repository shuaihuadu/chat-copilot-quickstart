namespace ChatCopilot.WebApi.Models.Response;

public class SpeechTokenResponse
{
    public string? Token { get; set; }

    public string? Region { get; set; }

    public bool? IsSuccess { get; set; }
}