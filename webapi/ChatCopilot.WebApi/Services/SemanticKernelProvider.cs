namespace ChatCopilot.WebApi.Services;

public sealed class SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private readonly IKernelBuilder _kernelBuilder = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory);

    public Kernel GetCompletionKernel() => this._kernelBuilder.Build();

    private static IKernelBuilder InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();

        builder.Services.AddLogging();

        KernelMemoryConfig kernelMemoryConfig = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (kernelMemoryConfig.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                AzureOpenAIConfig azureOpenAIConfig = kernelMemoryConfig.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: azureOpenAIConfig.Deployment,
                    endpoint: azureOpenAIConfig.Endpoint,
                    apiKey: azureOpenAIConfig.APIKey,
                    httpClient: httpClientFactory.CreateClient());

                break;
            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                OpenAIConfig openAIConfig = kernelMemoryConfig.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                builder.AddOpenAIChatCompletion(
                    modelId: openAIConfig.TextModel,
                    apiKey: openAIConfig.APIKey,
                    httpClient: httpClientFactory.CreateClient());

                break;
            default:
                break;
        }

        return builder;
    }
}
