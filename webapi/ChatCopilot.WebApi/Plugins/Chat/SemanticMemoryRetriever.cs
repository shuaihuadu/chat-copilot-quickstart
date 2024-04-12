namespace ChatCopilot.WebApi.Plugins.Chat;

public class SemanticMemoryRetriever
{
    private readonly PromptsOptions _promptOptions;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly IKernelMemory _kernelMemory;
    private readonly List<string> _memoryNames;
    private readonly ILogger _logger;

    public SemanticMemoryRetriever(
        IOptions<PromptsOptions> promptsOptions,
        IKernelMemory kernelMemory,
        ILogger logger,
        ChatSessionRepository chatSessionRepository)
    {
        this._promptOptions = promptsOptions.Value;
        this._chatSessionRepository = chatSessionRepository;
        this._kernelMemory = kernelMemory;
        this._logger = logger;

        this._memoryNames =
        [
            this._promptOptions.DocumentMemoryName,
            this._promptOptions.LongTermMemoryName,
            this._promptOptions.WorkingMemoryName
        ];
    }

    public async Task<(string, IDictionary<string, CitationSource>)> QueryMemoriesAsync(
        [Description("Query to match.")] string query,
        [Description("Chat ID to query history from")] string chatId,
        [Description("Maximum number of tokens")] int tokenLimit)
    {
        ChatSession? charSession = null;

        if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, callback: v => charSession = v))
        {
            throw new ArgumentException($"Chat session {chatId} not found.");
        }

        int remainingToken = tokenLimit;

        List<(Citation Citation, Citation.Partition Memory)> relevantMemories = [];

        List<Task> tasks = [];

        foreach (var memoryName in this._memoryNames)
        {
            tasks.Add(SearchMemoryAsync(memoryName));
        }

        tasks.Add(SearchMemoryAsync(this._promptOptions.DocumentMemoryName, isGlobalMemory: true));

        await Task.WhenAll(tasks);

        StringBuilder memoryBuilder = new();

        IDictionary<string, CitationSource> citationMap = new Dictionary<string, CitationSource>(StringComparer.OrdinalIgnoreCase);

        if (relevantMemories.Count > 0)
        {
            (var memoryMap, citationMap) = ProcessMemories();

            FormatMemories();
            FormatSnippets();

            void FormatMemories()
            {
                foreach (var memoryName in this._promptOptions.MemoryMap.Keys)
                {
                    if (memoryMap.TryGetValue(memoryName, out List<(string, CitationSource)>? memories))
                    {
                        foreach ((string memoryContent, _) in memories)
                        {
                            if (memoryBuilder.Length == 0)
                            {
                                memoryBuilder.Append("Past memories (format: [memory type] <label>: <details>):\n");
                            }

                            string memoryText = $"[{memoryName}] {memoryContent}\n";
                            memoryBuilder.Append(memoryContent);
                        }
                    }
                }
            }

            void FormatSnippets()
            {
                if (!memoryMap.TryGetValue(this._promptOptions.DocumentMemoryName, out List<(string, CitationSource)>? memories) || memories.Count == 0)
                {
                    return;
                }

                memoryBuilder.Append(
                    "User has also shared some document snippets.\n" +
                    "Quote the document link in square brackets at the end of each sentence that refers to the snippet in your response.\n");

                foreach ((string memoryContent, CitationSource citation) in memories)
                {
                    string memoryText = $"Document name:{citation.SourceName}\nDocument link:{citation.Link}.\n[CONTENT START]\n{memoryContent}\n[CONTENT END]\n";
                    memoryBuilder.Append(memoryText);
                }
            }
        }

        return (memoryBuilder.Length == 0 ? string.Empty : memoryBuilder.ToString(), citationMap);

        async Task SearchMemoryAsync(string memoryName, bool isGlobalMemory = false)
        {
            SearchResult? searchResult = await this._kernelMemory.SearchMemoryAsync(
                this._promptOptions.MemoryIndexName,
                query,
                this.CalculateRelevanceThreshold(memoryName, charSession!.MemoryBalance),
                isGlobalMemory ? DocumentMemoryOptions.GlobalDocumentChatId.ToString() : chatId,
                memoryName);

            foreach (var result in searchResult.Results.SelectMany(c => c.Partitions.Select(p => (c, p))))
            {
                relevantMemories.Add(result);
            }
        }

        (IDictionary<string, List<(string, CitationSource)>>, IDictionary<string, CitationSource>) ProcessMemories()
        {
            Dictionary<string, List<(string, CitationSource)>> memoryMap = new(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, CitationSource> citationMap = new(StringComparer.OrdinalIgnoreCase);

            foreach ((Citation Citation, Citation.Partition Memory) result in relevantMemories.OrderByDescending(m => m.Memory.Relevance))
            {
                int tokenCount = TokenUtils.TokenCount(result.Memory.Text);

                if (remainingToken - tokenCount > 0)
                {
                    if (result.Memory.Tags.TryGetValue(MemoryTags.TagMemory, out List<string?> tag) && tag.Count > 0)
                    {
                        string memoryName = tag.Single()!;

                        CitationSource citationSource = CitationSource.FromSemanticMemoryCitation(result.Citation, result.Memory.Text, result.Memory.Relevance);

                        if (this._memoryNames.Contains(memoryName))
                        {
                            if (!memoryMap.TryGetValue(memoryName, out List<(string, CitationSource)>? memories))
                            {
                                memories = [];
                                memoryMap.Add(memoryName, memories);
                            }

                            memories.Add((result.Memory.Text, citationSource));

                            remainingToken -= tokenCount;
                        }

                        if (memoryName == this._promptOptions.DocumentMemoryName)
                        {
                            citationMap.TryAdd(result.Citation.Link, citationSource);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            return (memoryMap, citationMap);
        }
    }


    private float CalculateRelevanceThreshold(string memoryName, float memoryBalance)
    {
        float upper = this._promptOptions.SemanticMemoryRelevanceUpper;
        float lower = this._promptOptions.SemanticMemoryRelevanceLower;

        if (memoryBalance < 0.0 || memoryBalance > 1.0)
        {
            throw new ArgumentException($"Invalid memory balance: {memoryBalance}");
        }

        if (memoryName == this._promptOptions.LongTermMemoryName)
        {
            return (lower - upper) * memoryBalance + upper;
        }
        else if (memoryName == this._promptOptions.WorkingMemoryName)
        {
            return (upper - lower) * memoryBalance + lower;
        }
        else if (memoryName == this._promptOptions.DocumentMemoryName)
        {
            return this._promptOptions.DocumentMemoryMinRelevance;
        }
        else
        {
            throw new ArgumentException($"Invalid memory name:{memoryName}");
        }
    }
}