namespace ChatCopilot.IntegrationTests;

public class HealthzTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async Task HealthzSuccessfullyReturns()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync("healthz");

        response.EnsureSuccessStatusCode();
    }
}