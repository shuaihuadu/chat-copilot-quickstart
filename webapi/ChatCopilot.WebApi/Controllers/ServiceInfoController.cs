namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ServiceInfoController : ControllerBase
{
    private readonly ILogger<ServiceInfoController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<Plugin> _availablePlugins;

    private readonly KernelMemoryConfig _memoryConfig;
    private readonly ChatAuthenticationOptions _chatAuthenticationOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ContentSafetyOptions _contentSafetyOptions;

    public ServiceInfoController(
        ILogger<ServiceInfoController> logger,
        IConfiguration configuration,
        IOptions<KernelMemoryConfig> memoryConfig,
        IOptions<ChatAuthenticationOptions> chatAuthenticationOptions,
        IOptions<FrontendOptions> frontendOptions,
        IOptions<ContentSafetyOptions> contentSafetyOptions,
        IDictionary<string, Plugin> availablePlugins)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._memoryConfig = memoryConfig.Value;
        this._chatAuthenticationOptions = chatAuthenticationOptions.Value;
        this._frontendOptions = frontendOptions.Value;
        this._contentSafetyOptions = contentSafetyOptions.Value;

        this._availablePlugins = this.SanitizePlugins(availablePlugins);
    }

    [HttpGet]
    [Route("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetServiceInfo()
    {
        ServiceInfoResponse response = new ServiceInfoResponse()
        {
            MemoryStore = new MemoryStoreInfoResponse()
            {
                Types = Enum.GetNames(typeof(MemoryStoreType)),
                SelectedType = this._memoryConfig.GetMemoryStoreType(this._configuration).ToString()
            },
            AvailablePlugins = this._availablePlugins,
            Version = GetAssemblyFileVersion(),
            IsContentSafetyEnabled = this._contentSafetyOptions.Enabled
        };

        return Ok(response);
    }

    [HttpGet]
    [Route("authConfig")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        string authorityUriString = string.Empty;

        if (!string.IsNullOrEmpty(this._chatAuthenticationOptions.AzureAd!.Instance)
            && !string.IsNullOrEmpty(this._chatAuthenticationOptions.AzureAd.TenantId))
        {
            Uri authorityUri = new(this._chatAuthenticationOptions.AzureAd.Instance);
            authorityUri = new(authorityUri, this._chatAuthenticationOptions.AzureAd.TenantId);
            authorityUriString = authorityUri.ToString();
        }

        FrontendAuthConfig config = new()
        {
            AuthType = this._chatAuthenticationOptions.Type.ToString(),
            AadAuthority = authorityUriString,
            AadClientId = this._frontendOptions.AadClientId,
            AadApiScope = $"api://{this._chatAuthenticationOptions.AzureAd.ClientId}/{this._chatAuthenticationOptions.AzureAd.Scopes}"
        };

        return Ok(config);
    }

    private static string GetAssemblyFileVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

        return fileVersion.FileVersion ?? string.Empty;
    }

    private IEnumerable<Plugin> SanitizePlugins(IDictionary<string, Plugin> plugins)
    {
        return plugins.Select(p => new Plugin()
        {
            Name = p.Value.Name,
            ManifestDomain = p.Value.ManifestDomain
        });
    }
}
