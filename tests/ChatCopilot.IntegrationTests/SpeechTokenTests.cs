using ChatCopilot.WebApi.Models.Response;
using System.Text.Json;

namespace ChatCopilot.IntegrationTests;

public class SpeechTokenTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async Task GetSpeechToken()
    {
        //await this.SetupAuth();

        HttpResponseMessage response = await this._httpClient.GetAsync("speechToken/");

        response.EnsureSuccessStatusCode();


        var contentStream = await response.Content.ReadAsStreamAsync();
        var speechTokenResponse = await JsonSerializer.DeserializeAsync<SpeechTokenResponse>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(speechTokenResponse);
        Assert.True((speechTokenResponse.IsSuccess == true && !string.IsNullOrEmpty(speechTokenResponse.Token)) ||
                     speechTokenResponse.IsSuccess == false);
    }
}
