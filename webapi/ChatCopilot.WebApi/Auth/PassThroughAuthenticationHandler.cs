namespace ChatCopilot.WebApi.Auth;

public class PassThroughAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "PassThrough";
    private const string DefaultUserId = "c05c61eb-65e4-4223-915a-fe72b0c9ece1";
    private const string DefaultUserName = "Default User";

    [Obsolete]
    public PassThroughAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        this.Logger.LogInformation("Allowing request to pass through");

        Claim userIdClaim = new(ClaimConstants.Sub, DefaultUserId);
        Claim nameClaim = new(ClaimConstants.Name, DefaultUserName);

        ClaimsIdentity identity = new([userIdClaim, nameClaim], AuthenticationScheme);

        ClaimsPrincipal principal = new(identity);

        AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
