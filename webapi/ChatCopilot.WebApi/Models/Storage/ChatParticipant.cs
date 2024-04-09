namespace ChatCopilot.WebApi.Models.Storage;

public class ChatParticipant : IStorageEntity
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string ChatId { get; set; }

    [JsonIgnore]
    public string Partition => this.UserId;

    public ChatParticipant(string userId, string chatId)
    {
        this.Id = Guid.NewGuid().ToString();
        this.UserId = userId;
        this.ChatId = chatId;
    }
}
