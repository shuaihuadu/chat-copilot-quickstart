namespace ChatCopilot.WebApi.Options;

public class PromptsOptions
{
    public const string PropertyName = "Prompts";

    [Required, Range(0, int.MaxValue)]
    public int CompletionTokenLimit { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int ResponseTokenLimit { get; set; }

    internal double MemoriesResponseContextWeight { get; } = 0.6;

    internal float SemanticMemoryRelevanceUpper { get; } = 0.9F;

    internal float SemanticMemoryRelevanceLower { get; } = 0.6F;

    internal float DocumentMemoryMinRelevance { get; } = 0.8F;

    [Required, NotEmptyOrWhitespace]
    public string KnowledgeCutoffDate { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string InitialBotMessage { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemDescription { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemResponse { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemAudience { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemAudienceContinuation { get; set; } = string.Empty;

    internal string[] SystemAudiencePromptComponents =>
    [
        this.SystemAudience,
        "{{ChatPlugin.ExtractChatHistory}}",
        this.SystemAudienceContinuation
    ];

    internal string SystemAudienceExtraction => string.Join("\n", this.SystemAudiencePromptComponents);

    [Required, NotEmptyOrWhitespace]
    public string SystemIntent { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemIntentContinuation { get; set; } = string.Empty;

    internal string[] SystemIntentPromptComponents =>
    [
        this.SystemDescription,
        this.SystemIntent,
        "{{ChatPlugin.ExtractChatHistory}}",
        this.SystemIntentContinuation
    ];

    internal string SystemIntentExtraction => string.Join("\n", this.SystemIntentPromptComponents);

    [Required, NotEmptyOrWhitespace]
    public string MemoryIndexName { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string DocumentMemoryName { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string SystemCognitive { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string MemoryFormat { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string MemoryAntiHallucination { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string MemoryContinuation { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string LongTermMemoryName { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string LongTermMemoryExtraction { get; set; } = string.Empty;

    internal string[] LongTermMemoryPromptComponents =>
    [
        this.SystemCognitive,
        $"{this.LongTermMemoryName} Description:\n{this.LongTermMemoryExtraction}",
        this.MemoryAntiHallucination,
        $"Chat Description:\n{this.SystemDescription}",
        "{{ChatPlugin.ExtractChatHistory}}",
        this.MemoryContinuation
    ];

    internal string LongTermMemory => string.Join("\n", this.LongTermMemoryPromptComponents);

    [Required, NotEmptyOrWhitespace]
    public string WorkingMemoryName { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string WorkingMemoryExtraction { get; set; } = string.Empty;

    internal string[] WorkingMemoryPromptComponents =>
    [
        this.SystemCognitive,
        $"{this.WorkingMemoryName} Description:\n{this.WorkingMemoryExtraction}",
        $"Chat Description:\n{this.SystemDescription}",
        "{{ChatPlugin.ExtractChatHistory}}",
        this.MemoryContinuation
    ];

    internal string WorkingMemory => string.Join("\n", this.WorkingMemoryPromptComponents);

    internal IDictionary<string, string> MemoryMap => new Dictionary<string, string>()
    {
        {this.LongTermMemoryName,this.LongTermMemory},
        {this.WorkingMemoryName,this.WorkingMemory}
    };

    internal string[] SystemPersonaComponents =>
    [
        this.SystemDescription,
        this.SystemResponse
    ];

    internal string SystemPersona => string.Join("\n\n", this.SystemPersonaComponents);

    internal double ResponseTemperature { get; } = 0.7;

    internal double ResponseTopP { get; } = 1;

    internal double ResponsePresencePenalty { get; } = 0.5;

    internal double ResponseFrequencyPenalty { get; } = 0.5;

    internal double IntentTemperature { get; } = 0.7;

    internal double IntentTopP { get; } = 1;

    internal double IntentPresencePenalty { get; } = 0.5;

    internal double IntentFrequencyPenalty { get; } = 0.5;

    internal PromptsOptions Copy() => (PromptsOptions)this.MemberwiseClone();

    internal bool TryGetMemoryContainerName(string memoryType, out string memoryContainerName)
    {
        memoryContainerName = "";

        if (!Enum.TryParse(memoryType, true, out SemanticMemoryType semanticMemoryType))
        {
            return false;
        }

        switch (semanticMemoryType)
        {
            case SemanticMemoryType.LongTermMemory:
                memoryContainerName = this.LongTermMemoryName;
                return true;

            case SemanticMemoryType.WorkingMemory:
                memoryContainerName = this.WorkingMemoryName;
                return true;

            default:
                return false;
        }
    }
}
