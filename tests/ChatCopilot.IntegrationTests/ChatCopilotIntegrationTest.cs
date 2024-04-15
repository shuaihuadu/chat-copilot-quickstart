using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace ChatCopilot.IntegrationTests;

[Trait("Catagory", "Integration Tests")]
public abstract class ChatCopilotIntegrationTest : IDisposable
{
    protected const string BaseUrlSettingName = "BaseServerUrl";
    protected const string ClientIdSettingName = "ClientID";
    protected const string AuthoritySettingName = "Authority";
    protected const string UsernameSettingName = "TestUsername";
    protected const string PasswordSettingName = "TestPassowrd";
    protected const string ScopesSettingName = "Scopes";

    protected readonly HttpClient _httpClient;
    protected readonly IConfigurationRoot configuration;

    protected ChatCopilotIntegrationTest()
    {
        this.configuration = new ConfigurationBuilder()
            .AddJsonFile(@"D:\appsettings\azure_ad.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<HealthzTests>()
            .Build();

        string? baseUrl = this.configuration[BaseUrlSettingName];

        Assert.False(string.IsNullOrEmpty(baseUrl));
        Assert.True(baseUrl.EndsWith('/'));

        this._httpClient = new HttpClient();
        this._httpClient.BaseAddress = new Uri(baseUrl);
    }

    protected async Task SetupAuth()
    {
        string accessToken = await this.GetUserTokenByPassword();

        Assert.True(!string.IsNullOrEmpty(accessToken));

        this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<string> GetUserTokenByPassword()
    {
        IPublicClientApplication app = PublicClientApplicationBuilder.Create(this.configuration[ClientIdSettingName])
            .WithAuthority(this.configuration[AuthoritySettingName])
            .Build();

        string? scopeString = this.configuration[ScopesSettingName];

        Assert.NotNull(scopeString);

        string[] scopes = scopeString.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        IEnumerable<IAccount> accounts = await app.GetAccountsAsync();

        AuthenticationResult? result = null;

        if (accounts.Any())
        {
            result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }
        else
        {
            result = await app.AcquireTokenByUsernamePassword(scopes, this.configuration[UsernameSettingName], this.configuration[PasswordSettingName]).ExecuteAsync();
        }

        return result?.AccessToken ?? string.Empty;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._httpClient.Dispose();
        }
    }
}