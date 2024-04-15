namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class SpeechTokenController : ControllerBase
{
    private sealed class TokenResult
    {
        public string? Token { get; set; }

        public HttpStatusCode? ResponseCode { get; set; }
    }

    private readonly ILogger<SpeechTokenController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureSpeechOptions _speechOptions;

    public SpeechTokenController(
        ILogger<SpeechTokenController> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<AzureSpeechOptions> speechOptions)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._speechOptions = speechOptions.Value;
    }

    [HttpGet]
    [Route("speechToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SpeechTokenResponse>> GetAsync()
    {
        if (string.IsNullOrWhiteSpace(this._speechOptions.Region)
            || string.IsNullOrWhiteSpace(this._speechOptions.Key))
        {
            return new SpeechTokenResponse { IsSuccess = false };
        }

        string fetchTokenUri = $"https://{this._speechOptions.Region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        TokenResult tokenResult = await this.FetchTokenAsync(fetchTokenUri, this._speechOptions.Key);

        bool isSuccess = tokenResult.ResponseCode != HttpStatusCode.NotFound;

        return new SpeechTokenResponse
        {
            Token = tokenResult.Token,
            Region = this._speechOptions.Region,
            IsSuccess = isSuccess
        };
    }

    private async Task<TokenResult> FetchTokenAsync(string fetchTokenUri, string key)
    {
        using HttpClient client = this._httpClientFactory.CreateClient();


        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, fetchTokenUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);

        HttpResponseMessage result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode)
        {
            HttpResponseMessage response = result.EnsureSuccessStatusCode();

            this._logger.LogDebug("Token Uri: {0}", fetchTokenUri);

            string token = await result.Content.ReadAsStringAsync();

            return new TokenResult { Token = token, ResponseCode = response.StatusCode };
        }

        return new TokenResult { ResponseCode = HttpStatusCode.NotFound };
    }
}
