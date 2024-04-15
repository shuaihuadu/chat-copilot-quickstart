namespace ChatCopilot.WebApi.Models.Response;

public class ServiceInfoResponse
{
    [JsonPropertyName("memoryStore")]
    public MemoryStoreInfoResponse MemoryStore { get; set; } = new MemoryStoreInfoResponse();

    [JsonPropertyName("availablePlugins")]
    public IEnumerable<Plugin> AvailablePlugins { get; set; } = [];

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("isContentSafetyEnabled")]
    public bool IsContentSafetyEnabled { get; set; } = false;
}

public class MemoryStoreInfoResponse
{
    [JsonPropertyName("types")]
    public IEnumerable<string> Types { get; set; } = [];

    [JsonPropertyName("selectedType")]
    public string SelectedType { get; set; } = string.Empty;
}