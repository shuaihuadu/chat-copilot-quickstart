﻿namespace ChatCopilot.WebApi.Extensions;

public static class ServiceExtensions
{
    internal static IServiceCollection AddOptions(this IServiceCollection services, ConfigurationManager configuration)
    {
        AddOptions<ServiceOptions>(ServiceOptions.PropertyName);

        AddOptions<ChatAuthenticationOptions>(ChatAuthenticationOptions.PropertyName);

        AddOptions<ChatStoreOptions>(ChatStoreOptions.PropertyName);

        AddOptions<AzureSpeechOptions>(AzureSpeechOptions.PropertyName);

        AddOptions<DocumentMemoryOptions>(DocumentMemoryOptions.PropertyName);

        AddOptions<PromptsOptions>(PromptsOptions.PropertyName);

        AddOptions<ContentSafetyOptions>(ContentSafetyOptions.PropertyName);

        AddOptions<KernelMemoryConfig>(MemoryConfiguration.KernelMemorySection);

        AddOptions<FrontendOptions>(FrontendOptions.PropertyName);

        return services;

        void AddOptions<TOptions>(string propertyName) where TOptions : class
        {
            services.AddOptions<TOptions>(configuration.GetSection(propertyName));
        }
    }

    internal static void AddOptions<TOptions>(this IServiceCollection services, IConfigurationSection section) where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .PostConfigure(TrimStringProperties);
    }

    internal static IServiceCollection AddPlugins(this IServiceCollection services, IConfiguration configuration)
    {
        List<Plugin> plugins = configuration.GetSection(Plugin.PropertyName).Get<List<Plugin>>() ?? [];

        ILogger<Program> logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

        logger.LogDebug("Found {0} plugins.", plugins.Count);

        Dictionary<string, Plugin> validatedPlugins = [];

        foreach (Plugin plugin in plugins)
        {
            if (validatedPlugins.ContainsKey(plugin.Name))
            {
                logger.LogWarning("Plugin '{0}' is defined more than once. Skipping...", plugin.Name);

                continue;
            }

            Uri pluginManifestUrl = PluginUtils.GetPluginManifestUri(plugin.ManifestDomain);

            using HttpRequestMessage request = new(HttpMethod.Post, pluginManifestUrl);

            request.Headers.Add("User-Agent", Telemetry.HttpUserAgent);

            try
            {
                logger.LogInformation("Adding plugin: {0}", plugin.Name);

                using HttpClient httpClient = new();

                HttpResponseMessage response = httpClient.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Plugin '{plugin.Name}' at '{pluginManifestUrl}' returned status code '{response.StatusCode}'.");
                }

                validatedPlugins.Add(plugin.Name, plugin);

                logger.LogInformation("Added plugin: {0}.", plugin.Name);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is AggregateException)
            {
                logger.LogWarning(ex, "Plugin '{0}' at {1} responded with error. Skipping...", plugin.Name, pluginManifestUrl);
            }
            catch (Exception ex) when (ex is UriFormatException)
            {
                logger.LogInformation("Plugin '{0}' at {1} is not a valid URL. Skipping...", plugin.Name, pluginManifestUrl);
            }
        }

        services.AddSingleton<IDictionary<string, Plugin>>(validatedPlugins);

        return services;
    }

    internal static IServiceCollection AddMaintenanceServices(this IServiceCollection services)
    {
        services.AddSingleton<IReadOnlyList<IMaintenanceAction>>(sp => []);

        return services;
    }

    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                        .WithMethods("POST", "GET", "PUT", "DELETE", "PATCH")
                        .AllowAnyHeader();
                    });
            });
        }

        return services;
    }

    internal static IServiceCollection AddPersistentChatStore(this IServiceCollection services)
    {
        IStorageContext<ChatSession> chatSessionStorageContext;
        ICopilotChatMessageStorageContext chatMessageStorageContext;
        IStorageContext<MemorySource> chatMemorySourceStorageContext;
        IStorageContext<ChatParticipant> chatParticipantStorageContext;

        ChatStoreOptions chatStoreOptions = services.BuildServiceProvider().GetRequiredService<IOptions<ChatStoreOptions>>().Value;

        switch (chatStoreOptions.Type)
        {
            case ChatStoreOptions.ChatStoreType.Volatile:
                chatSessionStorageContext = new VolatileContext<ChatSession>();
                chatMessageStorageContext = new VolatileCopilotChatMessageContext();
                chatMemorySourceStorageContext = new VolatileContext<MemorySource>();
                chatParticipantStorageContext = new VolatileContext<ChatParticipant>();
                break;
            case ChatStoreOptions.ChatStoreType.FileSystem:

                if (chatStoreOptions.FileSystem == null)
                {
                    throw new InvalidOperationException("ChatStore:FileSystem is required when ChatStore:Type is 'FileSystem'");
                }

                string fullPath = Path.GetFullPath(chatStoreOptions.FileSystem.FilePath);
                string directory = Path.GetDirectoryName(fullPath) ?? string.Empty;

                chatSessionStorageContext = new FileSystemContext<ChatSession>(new FileInfo(Path.Join(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_sessions{Path.GetExtension(fullPath)}")));
                chatMessageStorageContext = new FileSystemCopilotChatMessageContext(new FileInfo(Path.Join(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_messages{Path.GetExtension(fullPath)}")));
                chatMemorySourceStorageContext = new FileSystemContext<MemorySource>(new FileInfo(Path.Join(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_memorysources{Path.GetExtension(fullPath)}")));
                chatParticipantStorageContext = new FileSystemContext<ChatParticipant>(new FileInfo(Path.Join(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_participants{Path.GetExtension(fullPath)}")));
                break;
            case ChatStoreOptions.ChatStoreType.Cosmos:
                if (chatStoreOptions.Cosmos == null)
                {
                    throw new InvalidOperationException("ChatStore:Cosmos is required when ChatStore:Type is 'Cosmos'");
                }
                chatSessionStorageContext = new CosmosDbContext<ChatSession>(chatStoreOptions.Cosmos.ConnectionString, chatStoreOptions.Cosmos.Database, chatStoreOptions.Cosmos.ChatSessionsContainer);
                chatMessageStorageContext = new CosmosDbCopilotChatMessageContext(chatStoreOptions.Cosmos.ConnectionString, chatStoreOptions.Cosmos.Database, chatStoreOptions.Cosmos.ChatMessagesContainer);
                chatMemorySourceStorageContext = new CosmosDbContext<MemorySource>(chatStoreOptions.Cosmos.ConnectionString, chatStoreOptions.Cosmos.Database, chatStoreOptions.Cosmos.ChatMemorySourcesContainer);
                chatParticipantStorageContext = new CosmosDbContext<ChatParticipant>(chatStoreOptions.Cosmos.ConnectionString, chatStoreOptions.Cosmos.Database, chatStoreOptions.Cosmos.ChatParticipantsContainer);
                break;
            default:
                throw new InvalidOperationException("Invalid 'ChatStore' setting 'chatStoreConfig.Type'.");
        }

        services.AddSingleton(new ChatSessionRepository(chatSessionStorageContext));
        services.AddSingleton(new ChatMessageRepository(chatMessageStorageContext));
        services.AddSingleton(new ChatMemorySourceRepository(chatMemorySourceStorageContext));
        services.AddSingleton(new ChatParticipantRepository(chatParticipantStorageContext));

        return services;
    }

    internal static IServiceCollection AddChatCopilotAuthorization(this IServiceCollection services)
    {
        return services.AddScoped<IAuthorizationHandler, ChatParticipantAuthorizationHandler>()
            .AddAuthorizationCore(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.AddPolicy(AuthPolicyName.RequireChatParticipant, builder =>
                {
                    builder.RequireAuthenticatedUser()
                        .AddRequirements(new ChatParticipantRequirement());
                });
            });
    }

    internal static IServiceCollection AddChatCopilotAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthInfo, AuthInfo>();

        ChatAuthenticationOptions chatAuthenticationOptions = services.BuildServiceProvider().GetRequiredService<IOptions<ChatAuthenticationOptions>>().Value;

        switch (chatAuthenticationOptions.Type)
        {
            case ChatAuthenticationOptions.AuthenticationType.None:
                services.AddAuthentication(PassThroughAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, PassThroughAuthenticationHandler>(
                    authenticationScheme: PassThroughAuthenticationHandler.AuthenticationScheme,
                    configureOptions: null);

                break;
            case ChatAuthenticationOptions.AuthenticationType.AzureAd:
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(configuration.GetSection($"{ChatAuthenticationOptions.PropertyName}:AzureAd"));

                break;
            default:
                throw new InvalidOperationException($"Invalid authentication type '{chatAuthenticationOptions.Type}'.");
        }

        return services;
    }

    private static void TrimStringProperties<T>(T options) where T : class
    {
        Queue<object> targets = new();

        targets.Enqueue(options);

        while (targets.Count > 0)
        {
            object target = targets.Dequeue();

            Type targetType = target.GetType();

            foreach (PropertyInfo property in targetType.GetProperties())
            {
                if (property.PropertyType.IsEnum)
                {
                    continue;
                }

                if (property.GetIndexParameters().Length == 0)
                {
                    continue;
                }

                if (property.PropertyType.Namespace == "System"
                    && property.CanRead
                    && property.CanWrite)
                {
                    if (property.PropertyType == typeof(string)
                        && property.GetValue(target) != null)
                    {
                        property.SetValue(target, property.GetValue(target)!.ToString()!.Trim());
                    }
                }
                else
                {
                    if (property.GetValue(target) != null)
                    {
                        targets.Enqueue(property.GetValue(target)!);
                    }
                }
            }
        }
    }
}