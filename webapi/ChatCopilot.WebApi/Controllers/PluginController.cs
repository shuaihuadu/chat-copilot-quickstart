namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class PluginController : ControllerBase
{
    private const string PluginStateChanged = "PluginStateChanged";

    private readonly ILogger<PluginController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDictionary<string, Plugin> _availablePlugins;
    private readonly ChatSessionRepository _chatSessionRepository;

    public PluginController(
        ILogger<PluginController> logger,
        IHttpClientFactory httpClientFactory,
        IDictionary<string, Plugin> availablePlugins,
        ChatSessionRepository chatSessionRepository)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._availablePlugins = availablePlugins;
        this._chatSessionRepository = chatSessionRepository;
    }

    [HttpGet]
    [Route("pluginManifests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPluginManifest([FromQuery] Uri manifestDomain)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, PluginUtils.GetPluginManifestUri(manifestDomain));

        request.Headers.Add("User-Agent", "Semantic-Kernel");

        using HttpClient client = this._httpClientFactory.CreateClient();

        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return this.StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        return this.Ok(await response.Content.ReadAsStringAsync());
    }

    [HttpPut]
    [Route("chats/{chatId:guid}/plugins/{pluginName}/{enabled:bool}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> SetPluginStateAsync([FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        Guid chatId,
        string pluginName,
        bool enabled)
    {
        if (!this._availablePlugins.ContainsKey(pluginName))
        {
            return this.NotFound("Plugin not found.");
        }

        string chatIdString = chatId.ToString();

        ChatSession? chat = null;

        if (!(await this._chatSessionRepository.TryFindByIdAsync(chatIdString, callback: v => chat = v)) || chat == null)
        {
            return this.NotFound("Chat not found.");
        }

        if (enabled)
        {
            chat.EnabledPlugins.Add(pluginName);
        }
        else
        {
            chat.EnabledPlugins.Remove(pluginName);
        }

        await this._chatSessionRepository.UpsertAsync(chat);
        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(PluginStateChanged, chatIdString, pluginName, enabled);

        return this.NoContent();
    }
}
