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

        //AddOptions<KernelMemoryConfig>(MemoryCon)

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
