namespace ChatCopilot.WebApi.Services;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<IMaintenanceAction> _actions;
    private readonly IOptions<ServiceOptions> _serviceOptions;
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    private bool? _isInMaintenance;

    public MaintenanceMiddleware(
        RequestDelegate next,
        IReadOnlyList<IMaintenanceAction> actions,
        IOptions<ServiceOptions> serviceOptions,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        ILogger<MaintenanceMiddleware> logger)
    {
        this._next = next;
        this._actions = actions;
        this._serviceOptions = serviceOptions;
        this._messageRelayHubContext = messageRelayHubContext;
        this._logger = logger;
    }

    public async Task Invoke(HttpContext context, Kernel kernel)
    {
        if (this._isInMaintenance == null || this._isInMaintenance.Value)
        {
            this._isInMaintenance = await this.InspectMaintenanceActionAsync();
        }

        if (this._serviceOptions.Value.InMaintenance)
        {
            await this._messageRelayHubContext.Clients.All.SendAsync(MaintenanceController.GlobalSiteMaintenance, "Site undergoing maintenance...");
        }

        await this._next(context);
    }

    private async Task<bool> InspectMaintenanceActionAsync()
    {
        bool inMaintenance = false;

        foreach (var action in this._actions)
        {
            inMaintenance |= await action.InvokeAsync();
        }

        return inMaintenance;
    }
}