namespace ChatCopilot.WebApi.Options
{
    public class PluginOptions
    {
        public const string PropertyName = "Plugin";

        public string Name { get; set; } = string.Empty;

        public Uri ManifestDomainP { get; set; } = new Uri("http://localhost");

        public string Key { get; set; } = string.Empty;
    }
}
