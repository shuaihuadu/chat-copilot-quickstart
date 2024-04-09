namespace ChatCopilot.WebApi.Auth;

public class AuthInfo : IAuthInfo
{
    private record struct AuthData(string UserId, string UserName);

    private readonly Lazy<AuthData> _data;

    public AuthInfo(IHttpContextAccessor httpContextAccessor)
    {
        this._data = new Lazy<AuthData>(() =>
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            if (user is null)
            {
                throw new InvalidOperationException("HttpContext must be present to inspect auth info.");
            }

            Claim? userIdClaim = user.FindFirst(ClaimConstants.Oid)
            ?? user.FindFirst(ClaimConstants.ObjectId)
            ?? user.FindFirst(ClaimConstants.Sub)
            ?? user.FindFirst(ClaimConstants.NameIdentifierId);

            if (userIdClaim is null)
            {
                throw new CredentialUnavailableException("User Id was not present in the request token.");
            }

            Claim? tenantIdClaim = user.FindFirst(ClaimConstants.Tid)
            ?? user.FindFirst(ClaimConstants.TenantId);

            Claim? userNameClaim = user.FindFirst(ClaimConstants.Name);

            if (userNameClaim is null)
            {
                throw new CredentialUnavailableException("User name was not present in the request token.");
            }

            return new AuthData
            {
                UserId = (tenantIdClaim is null) ? userIdClaim.Value : string.Join(".", userIdClaim.Value, tenantIdClaim.Value),
                UserName = userNameClaim.Value
            };

        }, isThreadSafe: false);
    }

    public string UserId => this._data.Value.UserId;
    public string Name => this._data.Value.UserName;
}
