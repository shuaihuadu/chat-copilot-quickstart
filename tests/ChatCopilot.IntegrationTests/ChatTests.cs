using ChatCopilot.WebApi.Models.Request;
using ChatCopilot.WebApi.Models.Response;
using Microsoft.Graph;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatCopilot.IntegrationTests;

public class ChatTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async Task ChatMessagePostSuccessedsWithValidInput()
    {
        await this.SetupAuth();

        CreateChatParameters createChatParameters = new CreateChatParameters
        {
            Title = nameof(ChatMessagePostSuccessedsWithValidInput)
        };

        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("chats", createChatParameters);

        response.EnsureSuccessStatusCode();

        Stream contentStream = await response.Content.ReadAsStreamAsync();

        CreateChatResponse? createChatResponse = await JsonSerializer.DeserializeAsync<CreateChatResponse>(contentStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(createChatResponse);

        Ask ask = new()
        {
            Input = "Who is Satya Nadella?",
            Variables = [new("MessageType", ChatMessageType.Message.ToString())]
        };

        response = await this._httpClient.PostAsJsonAsync($"chats/{createChatResponse.ChatSession.Id}/messages", ask);
        response.EnsureSuccessStatusCode();

        contentStream = await response.Content.ReadAsStreamAsync();

        AskResult? askResult = await JsonSerializer.DeserializeAsync<AskResult>(contentStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(askResult);
        Assert.False(string.IsNullOrEmpty(askResult.Value));

        response = await this._httpClient.DeleteAsync($"chats/{createChatResponse.ChatSession.Id}");
        response.EnsureSuccessStatusCode();
    }
}
