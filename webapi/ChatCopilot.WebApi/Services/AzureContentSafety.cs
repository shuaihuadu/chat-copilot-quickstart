namespace ChatCopilot.WebApi.Services;

public record AnalysisResult([property: JsonPropertyName("category")] string Category, [property: JsonPropertyName("severity")] short Severity);

public record ImageContent([property: JsonPropertyName("content")] string Content);

public record ImageAnalysisRequest([property: JsonPropertyName("image")] ImageContent Image);

public sealed class AzureContentSafety : IContentSafetyService
{
    private const string HttpUserAgent = "Chat Copilot";

    private readonly string _endpoint;
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler? _httpClientHandler;

    public AzureContentSafety(string endpoint, string apiKey, HttpClientHandler httpClientHandler)
    {
        this._endpoint = endpoint;
        this._httpClient = new HttpClient(httpClientHandler);
        this._httpClient.DefaultRequestHeaders.Add("User-Agent", HttpUserAgent);
        this._httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    public AzureContentSafety(string endpoint, string apiKey)
    {
        this._endpoint = endpoint;
        this._httpClientHandler = new HttpClientHandler { CheckCertificateRevocationList = true };

        this._httpClient = new HttpClient(this._httpClientHandler);
        this._httpClient.DefaultRequestHeaders.Add("User-Agent", HttpUserAgent);
        this._httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    public List<string> ParseViolatedCatagories(ImageAnalysisResponse imageAnalysisResponse, short threshold)
    {
        List<string> violatedCatagories = [];

        foreach (var property in typeof(ImageAnalysisResponse).GetProperties())
        {
            AnalysisResult? analysisResult = property.GetValue(imageAnalysisResponse) as AnalysisResult;

            if (analysisResult != null && analysisResult.Severity >= threshold)
            {
                violatedCatagories.Add($"{analysisResult.Category} ({analysisResult.Severity})");
            }
        }

        return violatedCatagories;
    }

    public async Task<ImageAnalysisResponse> ImageAnalysisAsync(IFormFile formFile, CancellationToken cancellationToken)
    {
        string base64Image = await this.ConvertFormFileToBase64Async(formFile);

        string image = base64Image.Replace("data:image/png;base64,", "", StringComparison.InvariantCultureIgnoreCase)
            .Replace("data:image/jpeg;base64,", "", StringComparison.InvariantCultureIgnoreCase);

        ImageContent content = new(image);

        ImageAnalysisRequest imageAnalysisRequest = new(content);

        using HttpRequestMessage httpRequestMessage = new()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{this._endpoint}/contentsafety/image:analyze?api-version=2023-04-30-preview"),
            Content = new StringContent(JsonSerializer.Serialize(imageAnalysisRequest), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = await this._httpClient.SendAsync(httpRequestMessage, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode || body is null)
        {
            throw new KernelException($"[Content Safety] Failed to analyze image. {response.StatusCode}");
        }

        ImageAnalysisResponse? result = JsonSerializer.Deserialize<ImageAnalysisResponse>(body!);

        if (result is null)
        {
            throw new KernelException($"[Content Safety] Failed to analyze image. Details: {body}");
        }

        return result;
    }

    private async Task<string> ConvertFormFileToBase64Async(IFormFile formFile)
    {
        using MemoryStream memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);

        byte[] bytes = memoryStream.ToArray();

        return Convert.ToBase64String(bytes);
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
        this._httpClientHandler?.Dispose();
    }
}