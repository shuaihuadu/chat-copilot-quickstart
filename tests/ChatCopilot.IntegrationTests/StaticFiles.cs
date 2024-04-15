namespace ChatCopilot.IntegrationTests;

public class StaticFiles : ChatCopilotIntegrationTest
{
    [Fact]
    public async Task GetStaticFilesAsync()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync("swagger/index.html");

        response.EnsureSuccessStatusCode();

        Assert.True(response.Content.Headers.ContentLength > 1);

        response = await this._httpClient.GetAsync("favicon.ico");

        response.EnsureSuccessStatusCode();

        Assert.True(response.Content.Headers.ContentLength > 1);
    }
}