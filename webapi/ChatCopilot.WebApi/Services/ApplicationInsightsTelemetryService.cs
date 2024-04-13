namespace ChatCopilot.WebApi.Services;

public class ApplicationInsightsTelemetryService : ITelemetryService
{
    private const string UnknownUserId = "unauthenticated";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsTelemetryService(IHttpContextAccessor httpContextAccessor, TelemetryClient telemetryClient)
    {
        this._httpContextAccessor = httpContextAccessor;
        this._telemetryClient = telemetryClient;
    }

    public void TrackPluginFunction(string pluginName, string functionName, bool success)
    {
        Dictionary<string, string> properties = new(this.BuildDefaultProperties())
        {
            {"pluginName",pluginName},
            {"functionName",functionName},
            {"success",success.ToString() }
        };

        this._telemetryClient.TrackEvent("PluginFunction", properties);
    }

    private Dictionary<string, string> BuildDefaultProperties()
    {
        string userId = GetUserIdFromHttpContext(this._httpContextAccessor);

        return new Dictionary<string, string>
        {
            {"userId", userId }
        };
    }

    public static string GetUserIdFromHttpContext(IHttpContextAccessor httpContextAccessor)
    {
        HttpContext? context = httpContextAccessor.HttpContext;

        if (context == null)
        {
            return UnknownUserId;
        }

        ClaimsPrincipal user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return UnknownUserId;
        }

        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return UnknownUserId;
        }

        return userId;
    }
}