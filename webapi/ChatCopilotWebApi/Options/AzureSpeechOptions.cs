namespace ChatCopilot.WebApi.Options;

public sealed class AzureSpeechOptions
{
    public const string PropertyName = "AzureSpeech";

    public string Region { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}
