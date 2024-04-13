namespace ChatCopilot.WebApi.Plugins.Chat;

internal class SemanticChatMemoryExtractor
{
    public static async Task ExtraceSemanticMemoryAsync(
        string chatId,
        IKernelMemory kernelMemory,
        Kernel kernel,
        KernelArguments context,
        PromptsOptions promptOptions,
        ILogger<ChatPlugin> logger,
        CancellationToken cancellationToken)
    {
        foreach (string memoryType in Enum.GetNames(typeof(SemanticMemoryType)))
        {
            try
            {
                if (!promptOptions.TryGetMemoryContainerName(memoryType, out string memoryName))
                {
                    logger.LogInformation("Unable to extract kernel memory for invalid memory type {0}. Continuing...", memoryType);

                    continue;
                }

                SemanticChatMemory semanticChatMemory = await ExtractCognitiveMemoryAsync(memoryType, memoryName, logger);

                foreach (var item in semanticChatMemory.Items)
                {
                    await CreateMemoryAsync(memoryName, item.ToFormattedString());
                }
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                logger.LogInformation("Unable to extract kernel memory for {0}:{1}. Continuing...", memoryType, ex.Message);

                continue;
            }
        }

        async Task<SemanticChatMemory> ExtractCognitiveMemoryAsync(string memoryType, string memoryName, ILogger<ChatPlugin> logger)
        {
            if (!promptOptions.MemoryMap.TryGetValue(memoryName, out string? memoryPrompt))
            {
                throw new ArgumentException($"Memory name {memoryName} is not supported.");
            }

            int tokenLimit = promptOptions.CompletionTokenLimit;

            int remainingToken = tokenLimit - promptOptions.ResponseTokenLimit - TokenUtils.TokenCount(memoryPrompt);

            KernelArguments memoryExtractionArguments = new(context);
            memoryExtractionArguments["tokenLimit"] = remainingToken.ToString(new NumberFormatInfo());
            memoryExtractionArguments["memoryName"] = memoryName;
            memoryExtractionArguments["format"] = promptOptions.MemoryFormat;
            memoryExtractionArguments["knowledgeCutoff"] = promptOptions.KnowledgeCutoffDate;

            KernelFunction completionFunction = kernel.CreateFunctionFromPrompt(memoryPrompt);

            FunctionResult result = await completionFunction.InvokeAsync(kernel, memoryExtractionArguments, cancellationToken);

            string? tokenUsage = TokenUtils.GetFunctionTokenUsage(result, logger);

            if (tokenUsage is not null)
            {
                context[TokenUtils.GetFunctionKey($"SystemCognitive_{memoryType}")] = tokenUsage;
            }
            else
            {
                logger.LogError("Unable to determine token usage for {0}", $"SystemCognitive_{memoryType}");
            }

            SemanticChatMemory memory = SemanticChatMemory.FromJson(result.ToString());

            return memory;
        }

        async Task CreateMemoryAsync(string memoryName, string memory)
        {
            try
            {
                SearchResult searchResult = await kernelMemory.SearchMemoryAsync(
                    promptOptions.MemoryIndexName,
                    memory,
                    promptOptions.SemanticMemoryRelevanceUpper,
                    resultCount: 1,
                    chatId,
                    memoryName,
                    cancellationToken);

                if (searchResult.Results.Count == 0)
                {
                    await kernelMemory.StoreMemoryAsync(promptOptions.MemoryIndexName, chatId, memoryName, memory, cancellationToken);
                }
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                logger.LogError(ex, "Unexpected failure searching {0}", promptOptions.MemoryIndexName);
            }
        }
    }
}