namespace ChatCopilot.WebApi.Models.Response;

public class ImageAnalysisResponse
{
    [JsonPropertyName("hateResult")]
    public AnalysisResult? HateResult { get; set; }

    [JsonPropertyName("selfHarmResult")]
    public AnalysisResult? SelfHarmResult { get; set; }

    [JsonPropertyName("sexualResult")]
    public AnalysisResult? SexualResult { get; set; }

    [JsonPropertyName("violenceResult")]
    public AnalysisResult? ViolenceResult { get; set; }
}