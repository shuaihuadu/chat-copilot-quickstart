namespace ChatCopilot.WebApi.Utilities;

internal static class PluginUtils
{
    public static Uri GetPluginManifestUri(string manifestDomain)
    {
        Uri uri = new(Uri.UnescapeDataString(manifestDomain));

        return GetPluginManifestUri(uri);
    }

    public static Uri GetPluginManifestUri(Uri manifestDomain)
    {
        UriBuilder uriBuilder = new(manifestDomain)
        {
            Path = "/.well-known/ai-plugin.json"
        };

        return uriBuilder.Uri;
    }

    public static string SanitizePluginName(string name)
    {
        return name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
