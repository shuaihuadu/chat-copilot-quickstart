namespace ChatCopilot.WebApi.Options
{
    public class Plugin
    {
        public const string PropertyName = "Plugins";

        public string Name { get; set; } = string.Empty;

        public Uri ManifestDomain { get; set; } = new Uri("http://localhost");

        public string Key { get; set; } = string.Empty;
    }
}
