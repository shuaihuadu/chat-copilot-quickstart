using ChatCopilot.WebApi.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory.Diagnostics;

namespace ChatCopilot.WebApi.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddOptions(this IServiceCollection services, ConfigurationManager configuration)
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

    internal static IServiceCollection AddPersistentChatStore(this IServiceCollection services)
    {
        IStorageContext<ChatSession> chatSessionStorageContext;
        ICopilotChatMessageStorageContext copilotChatMessageStorageContext;
        IStorageContext<MemorySource> chatMemorySourceStorageContext;
        IStorageContext<ChatParticipant> chatParticipantStorageContext;

        ChatStoreOptions chatStoreOptions = services.BuildServiceProvider().GetRequiredService<IOptions<ChatStoreOptions>>().Value;

        switch (chatStoreOptions.Type)
        {
            case ChatStoreOptions.ChatStoreType.Volatile:
                chatSessionStorageContext = new VolatileContext<ChatSession>();
                copilotChatMessageStorageContext = new VolatileCopilotChatMessageContext();
                chatMemorySourceStorageContext = new VolatileContext<MemorySource>();
                chatParticipantStorageContext = new VolatileContext<ChatParticipant>();
                break;
            case ChatStoreOptions.ChatStoreType.FileSystem:
                break;
            case ChatStoreOptions.ChatStoreType.Cosmos:
                break;
            default:
                break;
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
