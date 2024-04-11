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

        return builder;
    }

    private static Task RegisterChatCopilotFunctionsAsync(IServiceProvider sp, Kernel kernel)
    {
        kernel.RegisterChatPlugin(sp);

        kernel.ImportPluginFromObject(new TimePlugin(), nameof(TimePlugin));

        return Task.CompletedTask;
    }

    private static Kernel RegisterChatPlugin(this Kernel kernel, IServiceProvider sp)
    {
        kernel.ImportPluginFromObject(
            new ChatPlugin(
                kernel,
                memoryClient: sp.GetRequiredService<IKernelMemory>(),
                chatMessageRepository: sp.GetRequiredService<ChatMessageRepository>(),
                chatSessionRepository: sp.GetRequiredService<ChatSessionRepository>(),
                messageRelayHubContext: sp.GetRequiredService<IHubContext<MessageRelayHub>>(),
                promptOptions: sp.GetRequiredService<IOptions<PromptsOptions>>(),
                documentImportOptions: sp.GetRequiredService<IOptions<DocumentMemoryOptions>>(),
                contentSafety: sp.GetRequiredService<AzureContentSafty>(),
                logger: sp.GetRequiredService<ILogger<ChatPlugin>>()
            ),
            nameof(ChatPlugin)
        );

        return kernel;
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

        services.AddSingleton<IContentSaftyService>(sp => new AzureContentSafty(contentSafetyOptions.Endpoint, contentSafetyOptions.Key));
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

    private static Task RegisterPluginsAsync(IServiceProvider sp, Kernel kernel)
    {
        ILogger? logger = kernel.LoggerFactory.CreateLogger(nameof(Kernel));

        ServiceOptions options = sp.GetRequiredService<IOptions<ServiceOptions>>().Value;

        if (!string.IsNullOrWhiteSpace(options.SemanticPluginsDirectory))
        {
            foreach (string subDir in Directory.GetDirectories(options.SemanticPluginsDirectory))
            {
                try
                {
                    kernel.ImportPluginFromPromptDirectory(options.SemanticPluginsDirectory, Path.GetFileName(subDir));
                }
                catch (KernelException ex)
                {
                    logger.LogError("Could not load plugin from {Directory}: {Message}", subDir, ex.Message);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(options.NativePluginsDirectory))
        {
            string[] pluginFiles = Directory.GetFiles(options.NativePluginsDirectory, "*.cs");

            foreach (string pluginFile in pluginFiles)
            {
                string className = Path.GetFileNameWithoutExtension(pluginFile);

                Assembly assembly = Assembly.GetExecutingAssembly();

                Type? classType = assembly.GetTypes().FirstOrDefault(t => t.Name.Contains(className, StringComparison.CurrentCultureIgnoreCase));

                if (classType != null)
                {
                    try
                    {
                        object? plugin = Activator.CreateInstance(classType);

                        kernel.ImportPluginFromObject(plugin!, classType.Name);
                    }
                    catch (KernelException ex)
                    {
                        logger.LogError("Could not load plugin from file {File}: {Details}", pluginFile, ex.Message);
                    }
                }
                else
                {
                    logger.LogError("Class type not found. Make sure the class type matches exactly with the file name {FileName}", className);
                }
            }
        }

        return Task.CompletedTask;
    }
}