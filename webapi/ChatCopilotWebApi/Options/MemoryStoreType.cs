namespace ChatCopilot.WebApi.Options;

public enum MemoryStoreType
{
    Volatile,
    TextFile,
    Qdrant,
    AzureAISearch
}

public static class MemoryStoreTypeExtensions
{
    public static MemoryStoreType GetMemoryStoreType(this KernelMemoryConfig kernelMemoryConfig, IConfiguration configuration)
    {
        string type = kernelMemoryConfig.Retrieval.MemoryDbType;

        if (type.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase))
        {
            return MemoryStoreType.AzureAISearch;
        }
        else if (type.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
        {
            return MemoryStoreType.Qdrant;
        }
        else if (type.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase))
        {
            SimpleVectorDbConfig simpleVectorDbConfig = kernelMemoryConfig.GetServiceConfig<SimpleVectorDbConfig>(configuration, "SimpleVectorDb");

            if (simpleVectorDbConfig != null)
            {
                type = simpleVectorDbConfig.StorageType.ToString();

                if (type.Equals("Volatile", StringComparison.OrdinalIgnoreCase))
                {
                    return MemoryStoreType.Volatile;
                }
                else if (type.Equals("Disk", StringComparison.OrdinalIgnoreCase))
                {
                    return MemoryStoreType.TextFile;
                }
            }
        }

        throw new ArgumentException($"Invalid memory store type: {type}");
    }
}