namespace ChatCopilot.WebApi.Services;

public interface IMaintenanceAction
{
    Task<bool> InvokeAsync(CancellationToken cancellationToken = default);
}