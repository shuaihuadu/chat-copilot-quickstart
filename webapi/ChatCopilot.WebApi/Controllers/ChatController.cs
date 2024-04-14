namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class ChatController : ControllerBase, IDisposable
{
    private readonly ILogger<ChatController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITelemetryService _telemetryService;
    private readonly ServiceOptions _serviceOptions;
    private readonly IDictionary<string, Plugin> _plugins;

    private readonly List<IDisposable> _disposables;

    private const string ChatPluginName = nameof(ChatPlugin);
    private const string ChatFunctionName = "Chat";
    private const string GeneratingResponseClientCall = "ReceiveBotResponseStatus";

    public ChatController(
        ILogger<ChatController> logger,
        IHttpClientFactory httpClientFactory,
        ITelemetryService telemetryService,
        IOptions<ServiceOptions> serviceOptions,
        IDictionary<string, Plugin> plugins)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._telemetryService = telemetryService;
        this._serviceOptions = serviceOptions.Value;
        this._plugins = plugins;
        this._disposables = [];
    }

    [Route("chats/{chatId:guid}/messages")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ChatAsync(
        [FromServices] Kernel kernel,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromServices] ChatSessionRepository chatSessionRepository,
        [FromServices] ChatParticipantRepository chatParticipantRepository,
        [FromServices] IAuthInfo authInfo,
        [FromBody] Ask ask,
        [FromRoute] Guid chatId)
    {
        this._logger.LogDebug("Chat message received.");

        string chatIdString = chatId.ToString();

        KernelArguments contextVariables = GetContextVariables(ask, authInfo, chatIdString);

        ChatSession? chat = null;

        if (!(await chatSessionRepository.TryFindByIdAsync(chatIdString, callback: c => chat = c)))
        {
            return this.NotFound("Failed to find chat session for the chatId specified in variables.");
        }

        if (!(await chatParticipantRepository.IsUserInChatAsync(authInfo.UserId, chatIdString)))
        {
            return this.Forbid("User does not have access to the chatId specified in variables.");
        }

        Dictionary<string, string> openApiPluginAuthHeaders = this.GetPluginAuthHeaders(this.HttpContext.Request.Headers);

        await this.RegisterFunctionsAsync(kernel, openApiPluginAuthHeaders, contextVariables);

        await this.RegisterHostedFunctionsAsync(kernel, chat!.EnabledPlugins);

        KernelFunction? chatFunction = kernel.Plugins.GetFunction(ChatPluginName, ChatFunctionName);

        FunctionResult? result = null;

        try
        {
            using CancellationTokenSource? cts = this._serviceOptions.TimeoutLimitInS is not null
                ? new CancellationTokenSource(TimeSpan.FromSeconds((double)this._serviceOptions.TimeoutLimitInS))
                : null;

            result = await kernel.InvokeAsync(chatFunction!, contextVariables, cts?.Token ?? default);

            this._telemetryService.TrackPluginFunction(ChatPluginName, ChatFunctionName, true);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException || ex.InnerException is OperationCanceledException)
            {
                this._logger.LogError("The {FunctionName} operation timed out.", ChatFunctionName);

                return this.StatusCode(StatusCodes.Status504GatewayTimeout, $"The chat {ChatFunctionName} timed out.");
            }

            this._telemetryService.TrackPluginFunction(ChatPluginName, ChatFunctionName, false);

            throw;
        }

        AskResult chatAskResult = new()
        {
            Value = result.ToString(),
            Variables = contextVariables.Select(v => new KeyValuePair<string, object?>(v.Key, v.Value))
        };

        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(GeneratingResponseClientCall, chatIdString, null);

        return this.Ok(chatAskResult);
    }

    private async Task RegisterFunctionsAsync(Kernel kernel, Dictionary<string, string> authHeaders, KernelArguments variables)
    {
        List<Task> tasks = [];

        if (authHeaders.TryGetValue("GITHUB", out string? GithubAuthHeader))
        {
            tasks.Add(this.RegisterGitHubPlugin(kernel, GithubAuthHeader));
        }

        if (authHeaders.TryGetValue("JIRA", out string? JiraAuthHeader))
        {
            tasks.Add(this.RegisterJiraPlugin(kernel, JiraAuthHeader, variables));
        }

        if (authHeaders.TryGetValue("GRAPH", out string? GraphAuthHeader))
        {
            tasks.Add(this.RegisterMicrosoftGraphPlugins(kernel, GraphAuthHeader));
        }

        if (variables.TryGetValue("customPlugins", out object? customPluginsString))
        {
            tasks.AddRange(this.RegisterCustomPlugins(kernel, customPluginsString, authHeaders));
        }

        await Task.WhenAll(tasks);
    }

    private async Task RegisterGitHubPlugin(Kernel kernel, string githubAuthHeader)
    {
        this._logger.LogInformation("Enabling Github plugin.");

        BearerAuthenticationProvider authenticationProvider = new(() => Task.FromResult(githubAuthHeader));

        await kernel.ImportPluginFromOpenApiAsync(
            pluginName: "GitHubPlugin",
            filePath: GetPluginFullPath("GithubPlugin/openapi.json"),
            new OpenApiFunctionExecutionParameters
            {
                AuthCallback = authenticationProvider.AuthenticateRequestAsync
            });
    }

    private async Task RegisterJiraPlugin(Kernel kernel, string jiraAuthHeader, KernelArguments variables)
    {
        this._logger.LogInformation("Registering Jira plugin");

        BasicAuthenticationProvider authenticationProvider = new(() => { return Task.FromResult(jiraAuthHeader); });

        bool hasServerUrlOverride = variables.TryGetValue("jira-server-url", out object? serverUrlOverride);

        await kernel.ImportPluginFromOpenApiAsync(
            pluginName: "JiraPlugin",
            filePath: GetPluginFullPath("OpenApi/JiraPlugin/openapi.json"),
            new OpenApiFunctionExecutionParameters
            {
                AuthCallback = authenticationProvider.AuthenticateRequestAsync,
                ServerUrlOverride = hasServerUrlOverride ? new Uri(serverUrlOverride!.ToString()!) : null
            });
    }

    private Task RegisterMicrosoftGraphPlugins(Kernel kernel, string graphAuthHeader)
    {
        this._logger.LogInformation("Enabling Microsoft Graph plugin(s).");

        BearerAuthenticationProvider basicAuthenticationProvider = new(() => Task.FromResult(graphAuthHeader));

        GraphServiceClient graphServiceClient = this.CreateGraphServiceClient(basicAuthenticationProvider.GraphClientAuthenticateRequestAsync);

        kernel.ImportPluginFromObject(new TaskListPlugin(new MicrosoftToDoConnector(graphServiceClient)), "todo");
        kernel.ImportPluginFromObject(new CalendarPlugin(new OutlookCalendarConnector(graphServiceClient)), "calendar");
        kernel.ImportPluginFromObject(new EmailPlugin(new OutlookMailConnector(graphServiceClient)), "email");

        return Task.CompletedTask;
    }

    private GraphServiceClient CreateGraphServiceClient(AuthenticateRequestAsyncDelegate authenticateRequestAsyncDelegate)
    {
        MsGraphClientLoggingHandler graphClientLoggingHandler = new(this._logger);

        this._disposables.Add(graphClientLoggingHandler);

        IList<DelegatingHandler> graphLoggingHandlers = GraphClientFactory.CreateDefaultHandlers(new DelegateAuthenticationProvider(authenticateRequestAsyncDelegate));

        graphLoggingHandlers.Add(graphClientLoggingHandler);

        HttpClient graphHttpClient = GraphClientFactory.Create(graphLoggingHandlers);

        this._disposables.Add(graphHttpClient);

        GraphServiceClient graphServiceClient = new(graphHttpClient);

        return graphServiceClient;
    }

    private IEnumerable<Task> RegisterCustomPlugins(Kernel kernel, object? customPluginsString, Dictionary<string, string> authHeaders)
    {
        CustomPlugin[]? customPlugins = JsonSerializer.Deserialize<CustomPlugin[]>(customPluginsString!.ToString()!);

        if (customPlugins is not null)
        {
            foreach (CustomPlugin plugin in customPlugins)
            {
                if (authHeaders.TryGetValue(plugin.AuthHeaderTag.ToLowerInvariant(), out string? pluginAuthValue))
                {
                    this._logger.LogInformation("Enabling {0} plugin.", plugin.NameForHuman);

                    bool requiresAuth = !plugin.AuthType.Equals("None", StringComparison.OrdinalIgnoreCase);

                    Task authCallback(HttpRequestMessage request, string _, OpenAIAuthenticationConfig __, CancellationToken ___ = default)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pluginAuthValue);

                        return Task.CompletedTask;
                    }

                    yield return kernel.ImportPluginFromOpenAIAsync(
                        $"{plugin.NameForModel}Plugin",
                        PluginUtils.GetPluginManifestUri(plugin.ManifestDomain),
                        new OpenAIFunctionExecutionParameters
                        {
                            HttpClient = this._httpClientFactory.CreateClient(),
                            IgnoreNonCompliantErrors = true,
                            AuthCallback = requiresAuth ? authCallback : null
                        });
                }
            }
        }
        else
        {
            this._logger.LogDebug("Failed to deserialize custom plugin details: {0}", customPluginsString);
        }
    }

    private async Task RegisterHostedFunctionsAsync(Kernel kernel, HashSet<string> enabledPlugins)
    {
        foreach (string enabledPlugin in enabledPlugins)
        {
            if (this._plugins.TryGetValue(enabledPlugin, out Plugin? plugin))
            {
                this._logger.LogDebug("Enabling hosted plugin {0}.", plugin.Name);

                Task authCallback(HttpRequestMessage request, string _, OpenAIAuthenticationConfig __, CancellationToken ___ = default)
                {
                    request.Headers.Add("X-Functions-Key", plugin.Key);

                    return Task.CompletedTask;
                }

                await kernel.ImportPluginFromOpenAIAsync(
                    PluginUtils.SanitizePluginName(plugin.Name),
                    PluginUtils.GetPluginManifestUri(plugin.ManifestDomain),
                    new OpenAIFunctionExecutionParameters
                    {
                        HttpClient = this._httpClientFactory.CreateClient(),
                        IgnoreNonCompliantErrors = true,
                        AuthCallback = authCallback
                    });
            }
            else
            {
                this._logger.LogWarning("Failed to find plugin {0}.", enabledPlugin);
            }
        }

        return;
    }

    private Dictionary<string, string> GetPluginAuthHeaders(IHeaderDictionary headers)
    {
        Regex regex = new("x-sk-copilot-(.*)-auth", RegexOptions.IgnoreCase);

        Dictionary<string, string> authHeaders = [];

        foreach (var header in headers)
        {
            Match match = regex.Match(header.Key);

            if (match.Success)
            {
                authHeaders.Add(match.Groups[1].Value.ToLowerInvariant(), header.Value!);
            }
        }

        return authHeaders;
    }

    private KernelArguments GetContextVariables(Ask ask, IAuthInfo authInfo, string chatIdString)
    {
        const string UserIdKey = "userId";
        const string UserNameKey = "userName";
        const string ChatIdKey = "chatId";
        const string MessageKey = "message";

        KernelArguments contextVariables = [];

        foreach (var variable in ask.Variables)
        {
            contextVariables[variable.Key] = variable.Value;
        }

        contextVariables[UserIdKey] = authInfo.UserId;
        contextVariables[UserNameKey] = authInfo.Name;
        contextVariables[ChatIdKey] = chatIdString;
        contextVariables[MessageKey] = ask.Input;

        return contextVariables;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (IDisposable disposable in this._disposables)
            {
                disposable.Dispose();
            }
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static string GetPluginFullPath(string pluginPath)
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Plugins", pluginPath);
    }
}

public class BasicAuthenticationProvider
{
    private readonly Func<Task<string>> _credentialsDelegate;

    public BasicAuthenticationProvider(Func<Task<string>> credentialsDelegate)
    {
        this._credentialsDelegate = credentialsDelegate;
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(await this._credentialsDelegate().ConfigureAwait(false)));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedContent);
    }
}

public class BearerAuthenticationProvider
{
    private readonly Func<Task<string>> _bearerTokenDelegate;

    public BearerAuthenticationProvider(Func<Task<string>> bearerTokenDelegate)
    {
        this._bearerTokenDelegate = bearerTokenDelegate;
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        string token = await this._bearerTokenDelegate().ConfigureAwait(false);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task GraphClientAuthenticateRequestAsync(HttpRequestMessage request)
    {
        await this.AuthenticateRequestAsync(request);
    }

    public async Task OpenAIAuthenticateRequestAsync(HttpRequestMessage request, string pluginName, OpenAIAuthenticationConfig openAIAuthenticationConfig, CancellationToken cancellationToken = default)
    {
        await this.AuthenticateRequestAsync(request, cancellationToken);
    }
}