namespace ChatCopilot.WebApi.Controllers;

public class MaintenanceController : ControllerBase
{
    internal const string GlobalSiteMaintenance = "GlobalSiteMaintenance";

    private readonly ILogger<MaintenanceController> _logger;
    private readonly IOptions<ServiceOptions> _serviceOptions;

    public MaintenanceController(
        ILogger<MaintenanceController> logger,
        IOptions<ServiceOptions> serviceOptions)
    {
        this._logger = logger;
        this._serviceOptions = serviceOptions;
    }

    [Route("maintenanceStatus")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MaintenanceResult?> GetMaintenanceStatusAsync(CancellationToken cancellationToken = default)
    {
        MaintenanceResult? result = null;

        if (this._serviceOptions.Value.InMaintenance)
        {
            result = new MaintenanceResult();
        }

        if (result != null)
        {
            return this.Ok(result);
        }

        return this.Ok();
    }
}
