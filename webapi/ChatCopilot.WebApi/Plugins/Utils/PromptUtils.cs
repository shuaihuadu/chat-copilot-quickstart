using static ChatCopilot.WebApi.Models.Storage.CopilotChatMessage;

namespace ChatCopilot.WebApi.Plugins.Utils;

public static class PromptUtils
{
    internal static string? FoormatChatHistoryMessage(AuthorRoles role, string content) => $"{role}: {content}";
}
