namespace ChatCopilot.WebApi.Services;

public class ApplicationInsightsUserTelemetryInitializerService : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _contextAccessor;

    public ApplicationInsightsUserTelemetryInitializerService(IHttpContextAccessor httpContextAccessor)
    {
        this._contextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is not RequestTelemetry requestTelemetry)
        {
            return;
        }

        string userId = ApplicationInsightsTelemetryService.GetUserIdFromHttpContext(this._contextAccessor);

        telemetry.Context.User.Id = userId;
    }
}