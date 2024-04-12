using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatCopilot.WebApi.Plugins.Utils;

public static class TokenUtils
{
    private static GptEncoding tokenizer = GptEncoding.GetEncoding("cl100k_base");

    internal static readonly Dictionary<string, string> semanticFunctions = new()
    {
        { "SystemAudienceExtraction","audienceExtraction" },
        { "SystemIntentExtraction","userIntentExtraction" },
        { "SystemMetaPrompt","metaPromptTemplate" },
        { "SystemCompletion","responseCompletion" },
        { "SystemCognitive_WorkingMemory","workingMemoryExtraction" },
        { "SystemCognitive_LongTermMemory","longTermMemoryExtraction" }
    };

    internal static Dictionary<string, int> EmptyTokenUsage()
    {
        return semanticFunctions.Values.ToDictionary(v => v, v => 0);
    }

    internal static string GetFunctionKey(string? functionName)
    {
        if (functionName == null || semanticFunctions.TryGetValue(functionName, out string? key))
        {
            throw new KeyNotFoundException($"Unknown token dependency {functionName}. Please define function as semanticFunctions entry in TokenUtils.cs");
        }

        return $"{key}TokenUsage";
    }

    internal static string? GetFunctionTokenUsage(FunctionResult result, ILogger logger)
    {
        if (result.Metadata is null ||
            !result.Metadata.TryGetValue("Usage", out object? usageObject)
            || usageObject is null)
        {
            logger.LogError("No usage metadata provided");

            return null;
        }

        int tokenUsage = 0;

        try
        {
            JsonElement jsonObject = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(usageObject));

            tokenUsage = jsonObject.GetProperty("TotalTokens").GetInt32();
        }
        catch (KeyNotFoundException)
        {
            logger.LogError("Usage details not found in model result.");
        }

        return tokenUsage.ToString(CultureInfo.InvariantCulture);
    }

    internal static int TokenCount(string text)
    {
        List<int> tokens = tokenizer.Encode(text);

        return tokens.Count;
    }

    internal static int GetContextMessageTokenCount(AuthorRole authorRole, string? content)
    {
        return TokenCount($"role:{authorRole.Label}") + TokenCount($"content:{content}\n");
    }

    internal static int GetContextMessageTokenCount(ChatHistory chatHistory)
    {
        int tokenCount = 0;

        foreach (var message in chatHistory)
        {
            tokenCount += GetContextMessageTokenCount(message.Role, message.Content);
        }

        return tokenCount;
    }
}
