namespace ChatCopilot.WebApi.Services;

public interface ITelemetryService
{
    void TrackPluginFunction(string pluginName, string functionName, bool success);
}