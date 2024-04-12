using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;

namespace ChatCopilot.Shared;

internal sealed class ServiceConfiguration
{
    private IConfiguration _rawAppSettings;
    private KernelMemoryConfig _kernelMemoryConfig;

    private const string ConfigRoot = "KernelMemory";
    private const string AspnetEnvVar = "ASPNETCORE_ENVIRONMENT";
    private const string OpenAIEnvVar = "OPENAI_API_KEY";

    public ServiceConfiguration(string? settingsDirectory = null) : this(ReadAppSettings(settingsDirectory)) { }

    public ServiceConfiguration(IConfiguration rawAppSettings) : this(rawAppSettings, rawAppSettings.GetSection(ConfigRoot).Get<KernelMemoryConfig>()
        ?? throw new ConfigurationException($"Unable to load Kernel Memory settings from the given configuration. " +
                                            $"There should be a '{ConfigRoot}' root node, " +
                                            $"with data mapping to '{nameof(KernelMemoryConfig)}'"))
    {

    }

    public ServiceConfiguration(IConfiguration rawAppSettings, KernelMemoryConfig kernelMemoryConfig)
    {
        this._rawAppSettings = rawAppSettings ?? throw new ConfigurationException("The given app settings configuration is NULL");
        this._kernelMemoryConfig = kernelMemoryConfig ?? throw new ConfigurationException("The giiven memory configuration is NULL");

        if (!this.MinimumConfigurationIsAvailable(false))
        {
            this.SetupForOpenAI();
        }

        this.MinimumConfigurationIsAvailable(true);
    }

    public IKernelMemoryBuilder PrepareBuilder(IKernelMemoryBuilder builder)
    {
        return this.BuildUsingConfiguration(builder);
    }

    private IKernelMemoryBuilder BuildUsingConfiguration(IKernelMemoryBuilder builder)
    {
        if (this._kernelMemoryConfig == null)
        {
            throw new ConfigurationException("The given memory configuration is NULL");
        }

        if (this._rawAppSettings == null)
        {
            throw new ConfigurationException("The given app settings configuration is NULL");
        }

        builder.AddSingleton(this._kernelMemoryConfig);

        this.ConfigureMimeTypeDetectionDependency(builder);

        this.ConfigureTextPartitioning(builder);

        this.ConfigureQueueDependency(builder);

        this.ConfigureStorageDependency(builder);

        this.ConfigureIngestionEmbeddingGenerators(builder);

        this.ConfigureSearchClient(builder);

        this.ConfigureRetrievalEmbeddingGenerator(builder);

        this.ConfigureIngestionMemoryDb(builder);

        this.ConfigureRetrievalMemoryDb(builder);

        this.ConfigureTextGenerator(builder);

        this.ConfigureImageOCR(builder);

        return builder;
    }

    private void ConfigureStorageDependency(IKernelMemoryBuilder builder)
    {
        switch (this._kernelMemoryConfig.ContentStorageType)
        {
            case string x1 when x1.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase):
            case string x2 when x2.Equals("AzureBlobs", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureBlobsAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>("AzureBlob") ?? this.GetServiceConfig<AzureBlobsConfig>("AzureBlobs"));
                break;
            case string x when x.Equals("SimpleFileStorage", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>("SimpleFileStorage"));
                break;
            default:
                break;
        }
    }

    private void ConfigureQueueDependency(IKernelMemoryBuilder builder)
    {
        if (string.Equals(this._kernelMemoryConfig.DataIngestion.OrchestrationType, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            switch (this._kernelMemoryConfig.DataIngestion.DistributedOrchestration.QueueType)
            {
                case string x1 when x1.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                case string x2 when x2.Equals("AzureQueues", StringComparison.OrdinalIgnoreCase):

                    builder.Services.AddAzureQueuesOrchestration(this.GetServiceConfig<AzureQueuesConfig>("AzureQueues") ?? this.GetServiceConfig<AzureQueuesConfig>("AzureQueue"));
                    break;
                case string x when x.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    builder.Services.AddRabbitMQOrchestration(this.GetServiceConfig<RabbitMqConfig>("RabbitMq"));
                    break;
                case string x when x.Equals("SimpleQueues", StringComparison.OrdinalIgnoreCase):
                    builder.Services.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>("SimpleQueues"));
                    break;
                default:
                    break;
            }
        }
    }

    private void ConfigureTextPartitioning(IKernelMemoryBuilder builder)
    {
        if (this._kernelMemoryConfig.DataIngestion.TextPartitioning != null)
        {
            this._kernelMemoryConfig.DataIngestion.TextPartitioning.Validate();

            builder.WithCustomTextPartitioningOptions(this._kernelMemoryConfig.DataIngestion.TextPartitioning);
        }
    }

    private void ConfigureMimeTypeDetectionDependency(IKernelMemoryBuilder builder)
    {
        builder.WithDefaultMimeTypeDetection();
    }

    private static IConfiguration ReadAppSettings(string? settingsDirectory)
    {
        ConfigurationBuilder builder = new();

        builder.AddKMConfigurationSources(settingsDirectory: settingsDirectory);

        return builder.Build();
    }

    private bool MinimumConfigurationIsAvailable(bool throwOnError)
    {
        if (string.IsNullOrEmpty(this._kernelMemoryConfig.TextGeneratorType))
        {
            if (!throwOnError)
            {
                return false;
            }

            throw new ConfigurationException("Text generation (TextGeneratorType) is not configured in Kernel Memory.");
        }

        if (this._kernelMemoryConfig.DataIngestion.EmbeddingGenerationEnabled)
        {
            if (this._kernelMemoryConfig.DataIngestion.EmbeddingGeneratorTypes.Count == 0)
            {
                if (!throwOnError)
                {
                    return false;
                }

                throw new ConfigurationException("Data ingestion embedding generation (DataIngestion.EmbeddingGeneratorTypes) is not configured in Kernel Memory.");
            }
        }

        if (string.IsNullOrEmpty(this._kernelMemoryConfig.Retrieval.EmbeddingGeneratorType))
        {
            if (!throwOnError)
            {
                return false;
            }

            throw new ConfigurationException("Retrieval embedding generation (Retrieval.EmbeddingGeneratorType) is not configured in Kernel Memory.");
        }

        return true;
    }

    private void SetupForOpenAI()
    {
        string openAIKey = Environment.GetEnvironmentVariable(OpenAIEnvVar)?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(openAIKey))
        {
            return;
        }

        Dictionary<string, string?> inMemoryConfig = new()
        {
            { $"{ConfigRoot}:Services:OpenAI:APIKey",openAIKey },
            { $"{ConfigRoot}:TextGeneratorType","OpenAI"},
            { $"{ConfigRoot}:DataIngestion:EmbeddingGeneratorTypes:0","OpenAI"},
            { $"{ConfigRoot}:Retrieval:EmbeddingGeneratorType","OpenAI" }
        };

        ConfigurationBuilder newAppSettings = new();

        newAppSettings.AddConfiguration(this._rawAppSettings);
        newAppSettings.AddInMemoryCollection(inMemoryConfig);

        this._rawAppSettings = newAppSettings.Build();
        this._kernelMemoryConfig = this._rawAppSettings.GetSection(ConfigRoot).Get<KernelMemoryConfig>()!;
    }

    private T GetServiceConfig<T>(string serviceName)
    {
        return this._kernelMemoryConfig.GetServiceConfig<T>(this._rawAppSettings, serviceName);
    }
}