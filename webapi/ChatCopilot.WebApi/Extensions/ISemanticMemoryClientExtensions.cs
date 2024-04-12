namespace ChatCopilot.WebApi.Extensions;

internal static class ISemanticMemoryClientExtensions
{
    private static readonly List<string> pipelineSteps = ["extract", "partition", "gen_embeddings", "save_records"];

    public static void AddSemanticMemoryServices(this WebApplicationBuilder appBuilder)
    {
        ServiceProvider serviceProvider = appBuilder.Services.BuildServiceProvider();

        KernelMemoryConfig kernelMemoryConfig = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        string ocrType = kernelMemoryConfig.DataIngestion.ImageOcrType;
        bool hasOcr = !string.IsNullOrWhiteSpace(ocrType) && !ocrType.Equals(MemoryConfiguration.NoneType, StringComparison.OrdinalIgnoreCase);

        string pipelineType = kernelMemoryConfig.DataIngestion.OrchestrationType;
        bool isDistributed = pipelineType.Equals(MemoryConfiguration.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase);

        appBuilder.Services.AddSingleton(sp => new DocumentTypeProvider(hasOcr));

        KernelMemoryBuilder memoryBuilder = new(appBuilder.Services);

        if (isDistributed)
        {
            memoryBuilder.WithoutDefaultHandlers();
        }
        else
        {
            if (hasOcr)
            {
                memoryBuilder.WithCustomOcr(appBuilder.Configuration);
            }
        }

        IKernelMemory kernelMemory = memoryBuilder
            .FromMemoryConfiguration(kernelMemoryConfig, appBuilder.Configuration)
            .Build();

        appBuilder.Services.AddSingleton(kernelMemory);
    }

    public static Task<SearchResult> SearchMemoryAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string query,
        float relevanceThreshold,
        string chatId,
        string? memoryName = null,
        CancellationToken cancellationToken = default)
    {
        return kernelMemory.SearchMemoryAsync(indexName, query, relevanceThreshold, resultCount: -1, chatId, memoryName, cancellationToken);
    }

    public static async Task<SearchResult> SearchMemoryAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string query,
        float relevanceThreshold,
        int resultCount,
        string chatId,
        string? memoryName = null,
        CancellationToken cancellationToken = default)
    {
        MemoryFilter filter = [];

        filter.ByTag(MemoryTags.TagChatId, chatId);

        if (!string.IsNullOrWhiteSpace(memoryName))
        {
            filter.ByTag(MemoryTags.TagMemory, memoryName);
        }

        SearchResult searchResult = await kernelMemory.SearchAsync(query, indexName, filter, null, relevanceThreshold, resultCount, cancellationToken);

        return searchResult;
    }

    public static async Task StoreDocumentAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string documentId,
        string chatId,
        string memoryName,
        string fileName,
        Stream fileContent,
        CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = new()
        {
            DocumentId = documentId,
            Files = [new(fileName, fileContent)],
            Index = indexName,
            Steps = pipelineSteps
        };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await kernelMemory.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    public static Task StoreMemoryAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string chatId,
        string memoryName,
        string memory,
        CancellationToken cancellationToken = default)
    {
        return kernelMemory.StoreMemoryAsync(indexName, chatId, memoryName, memoryId: Guid.NewGuid().ToString(), memory, cancellationToken);
    }

    public static async Task StoreMemoryAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string chatId,
        string memoryName,
        string memoryId,
        string memory,
        CancellationToken cancellationToken = default)
    {
        using MemoryStream stream = new MemoryStream();
        using StreamWriter writer = new StreamWriter(stream);

        await writer.WriteAsync(memory);
        await writer.FlushAsync();

        stream.Position = 0;

        DocumentUploadRequest uploadRequest = new()
        {
            DocumentId = memoryId,
            Index = indexName,
            Files = [new DocumentUploadRequest.UploadedFile("memory.txt", stream)],
            Steps = pipelineSteps
        };

        uploadRequest.Tags.Add(MemoryTags.TagChatId, chatId);
        uploadRequest.Tags.Add(MemoryTags.TagMemory, memoryName);

        await kernelMemory.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    public static async Task RemoveChatMemoriesAsync(
        this IKernelMemory kernelMemory,
        string indexName,
        string chatId,
        CancellationToken cancellationToken = default)
    {
        SearchResult? memories = await kernelMemory.SearchMemoryAsync(indexName, "*", 0.0F, chatId, cancellationToken: cancellationToken);

        string[] documentIds = memories.Results.Select(memory => memory.Link.Split('/').First()).Distinct().ToArray();

        Task[] tasks = documentIds.Select(documentId => kernelMemory.DeleteDocumentAsync(documentId, indexName, cancellationToken)).ToArray();

        Task.WaitAll(tasks, cancellationToken);
    }
}
