using Microsoft.AspNetCore.Mvc;

namespace ChatCopilot.WebApi.Controllers;

public class MaintenanceController : Controller
{
    internal const string GlobalSiteMaintenance = "GlobalSiteMaintenance";

    private readonly ILogger<MaintenanceController> _logger;
    private readonly IOptions<ServiceOptions> _serviceOptions;

    public MaintenanceController()
    {

    }
}
