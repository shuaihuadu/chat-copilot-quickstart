using ChatCopilot.WebApi.Models.Response;
using ChatCopilot.WebApi.Options;
using System.Text.Json;

namespace ChatCopilot.IntegrationTests;

public class ServiceInfoTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async Task GetServiceInfo()
    {
        //await this.SetupAuth();

        HttpResponseMessage response = await this._httpClient.GetAsync("info/");

        response.EnsureSuccessStatusCode();

        Stream contentStream = await response.Content.ReadAsStreamAsync();

        ServiceInfoResponse? objectFromResponse = await JsonSerializer.DeserializeAsync<ServiceInfoResponse>(contentStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(objectFromResponse);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.MemoryStore.SelectedType));
        Assert.False(string.IsNullOrEmpty(objectFromResponse.Version));
    }

    [Fact]
    public async Task GetAuthConfig()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync("authConfig/");

        response.EnsureSuccessStatusCode();

        Stream contentStream = await response.Content.ReadAsStreamAsync();

        FrontendAuthConfig? objectFromResponse = await JsonSerializer.DeserializeAsync<FrontendAuthConfig>(contentStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(objectFromResponse);
        Assert.Equal(ChatAuthenticationOptions.AuthenticationType.AzureAd.ToString(), objectFromResponse.AuthType);
        Assert.Equal(this.configuration[AuthoritySettingName], objectFromResponse.AadAuthority);
        Assert.Equal(this.configuration[ClientIdSettingName], objectFromResponse.AadClientId);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.AadApiScope));
    }
}
