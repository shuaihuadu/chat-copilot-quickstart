namespace ChatCopilot.WebApi.Options;

public class ChatAuthenticationOptions
{
    public const string PropertyName = "Authentication";

    public enum AuthenticationType
    {
        None,
        AzureAd
    }

    [Required]
    public AuthenticationType Type { get; set; } = AuthenticationType.None;

    [RequiredOnPropertyValue(nameof(Type), AuthenticationType.AzureAd)]
    public AzureAdOptions? AzureAd { get; set; }

    public class AzureAdOptions
    {
        [Required, NotEmptyOrWhitespace]
        public string Instance { get; set; } = string.Empty;

        [Required, NotEmptyOrWhitespace]
        public string TenantId { get; set; } = string.Empty;

        [Required, NotEmptyOrWhitespace]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string? Scopes { get; set; } = string.Empty;
    }
}
