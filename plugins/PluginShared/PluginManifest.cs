using Plugins.PluginShared;
using System.Text.Json.Serialization;

namespace Plugns.PluginShared;

public class PluginManifest
{

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "v1";

    [JsonPropertyName("name_for_model")]
    public string NameForModel { get; set; } = string.Empty;

    [JsonPropertyName("name_for_human")]
    public string NameForHuman { get; set; } = string.Empty;

    [JsonPropertyName("description_for_model")]
    public string DescriptionForModel { get; set; } = string.Empty;

    [JsonPropertyName("description_for_human")]
    public string DescriptionForHuman { get; set; } = string.Empty;

    public PluginAuth Auth { get; set; } = new PluginAuth();

    public PluginApi Api { get; set; } = new PluginApi();

    [JsonPropertyName("logo_url")]
    public string LogoUrl { get; set; } = string.Empty;

    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [JsonPropertyName("legal_info_url")]
    public string LegalInfoUrl { get; set; } = string.Empty;

    public string HttpAuthorizationType { get; set; } = string.Empty;
}
