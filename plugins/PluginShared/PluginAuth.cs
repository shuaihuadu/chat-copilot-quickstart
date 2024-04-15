using System.Text.Json.Serialization;

namespace Plugins.PluginShared;

public class PluginAuth
{
    public class VerificationTokens
    {
        public string OpenAI { get; set; } = string.Empty;
    }

    public string Type { get; set; } = "none";

    [JsonPropertyName("authorization_type")]
    public string AuthorizationType { get; } = "bearer";

    [JsonPropertyName("verification_tokens")]
    public VerificationTokens Tokens { get; set; } = new VerificationTokens();
}
