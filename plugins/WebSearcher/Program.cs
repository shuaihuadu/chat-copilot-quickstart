using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Plugins.WebSearcher.Models;
using System.Text.Json;

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(configuration =>
    {
        configuration.AddJsonFile(@"D:\appsettings\test_configuration.json", true, true);
    })
    .ConfigureServices(services =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        BingConfig? bingConfig = services.BuildServiceProvider().GetService<IConfiguration>()?.GetSection(BingConfig.SectionName).Get<BingConfig>();

        services.AddSingleton(bingConfig!);

        services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
        {
            OpenApiConfigurationOptions options = new OpenApiConfigurationOptions
            {
                Info = new OpenApiInfo()
                {
                    Version = "1.0.0",
                    Title = "Web Search Plugin",
                    Description = "This plugin is capable of searching the internet."
                },
                Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                OpenApiVersion = OpenApiVersionType.V3,
                IncludeRequestingHostName = true,
                ForceHttps = false,
                ForceHttp = false
            };

            return options;
        });
    })
    .Build();

host.Run();