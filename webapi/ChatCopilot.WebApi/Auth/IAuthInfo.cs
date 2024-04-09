namespace ChatCopilot.WebApi.Auth;

public interface IAuthInfo
{
    public string UserId { get; }

    public string Name { get; }
}
