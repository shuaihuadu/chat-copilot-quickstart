namespace ChatCopilot.WebApi.Extensions;

internal static class SemanticKernelExtensions
{
    public delegate Task RegisterFunctionsWithKernel(IServiceProvider sp, Kernel kernel);

    public delegate Task KernelSetupHook(IServiceProvider sp, Kernel kernel);

    public static WebApplicationBuilder AddSemanticKernelServices(this WebApplicationBuilder builder)
    {
        builder.InitializeKernelProvider();

        builder.Services.AddScoped<Kernel>(sp =>
        {
            SemanticKernelProvider provider = sp.GetRequiredService<SemanticKernelProvider>();

            Kernel kernel = provider.GetCompletionKernel();

            sp.GetRequiredService<RegisterFunctionsWithKernel>()(sp, kernel);

            sp.GetService<KernelSetupHook>()?.Invoke(sp, kernel);

            return kernel;
        });

        builder.Services.AddContentSafty();

        builder.Services.AddScoped<RegisterFunctionsWithKernel>(sp => RegisterChatCopilotFunctionsAsync);
    }

    private static async Task RegisterChatCopilotFunctionsAsync(IServiceProvider sp, Kernel kernel)
    {
        kernel.RegisterChatPlugin(sp);

        kernel.ImportPluginFromObject(new TimePlugin(), nameof(TimePlugin));
    }

    private static void InitializeKernelProvider(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(sp => new SemanticKernelProvider(sp, builder.Configuration, sp.GetRequiredService<IHttpClientFactory>()));
    }

    private static void AddContentSafty(this IServiceCollection services)
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        ContentSafetyOptions contentSafetyOptions = configuration.GetSection(ContentSafetyOptions.PropertyName).Get<ContentSafetyOptions>()
            ?? new ContentSafetyOptions { Enabled = false };

        services.AddSingleton<IContentS>
    }

    public static WebApplicationBuilder AddBotConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped(sp => sp.WithBotConfig(builder.Configuration));

        return builder;
    }

    private static ChatArchiveEmbeddingConfig WithBotConfig(this IServiceProvider provider, IConfiguration configuration)
    {
        KernelMemoryConfig kernelMemoryConfig = provider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (kernelMemoryConfig.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                AzureOpenAIConfig azureOpenAIConfig = kernelMemoryConfig.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIEmbedding");

                return new ChatArchiveEmbeddingConfig
                {
                    AIService = ChatArchiveEmbeddingConfig.AIServiceType.AzureOpenAIEmbedding,
                    DeploymentOrModelId = azureOpenAIConfig.Deployment
                };
            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                OpenAIConfig openAIConfig = kernelMemoryConfig.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                return new ChatArchiveEmbeddingConfig
                {
                    AIService = ChatArchiveEmbeddingConfig.AIServiceType.OpenAI,
                    DeploymentOrModelId = openAIConfig.EmbeddingModel
                };

            default:
                throw new ArgumentException($"Invalid {nameof(kernelMemoryConfig.Retrieval.EmbeddingGeneratorType)} value in 'SemanticMemory' settings.");
        }
    }
}