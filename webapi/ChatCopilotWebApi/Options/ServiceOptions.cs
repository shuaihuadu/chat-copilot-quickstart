namespace ChatCopilot.WebApi.Options;

public class ServiceOptions
{
    public const string PropertyName = "Service";

    [Range(0, int.MaxValue)]
    public double? TimeoutLimitIns { get; set; }

    [Url]
    public string? KeyVault { get; set; }

    public string? SemanticPluginsDirectory { get; set; }

    public string? NativePluginsDirectory { get; set; }

    public bool InMaintenance { get; set; }
}
