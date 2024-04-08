namespace ChatCopilot.WebApi.Options;

public class ContentSafetyOptions
{
    public const string PropertyName = "ContentSafety";

    [Required]
    public bool Enabled { get; set; }

    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Endpoint { get; set; } = string.Empty;

    [RequiredOnPropertyValue(nameof(Enabled), true)]
    public string Key { get; set; } = string.Empty;

    [Range(0, 6)]
    public short ViolationThreshold { get; set; } = 4;
}
