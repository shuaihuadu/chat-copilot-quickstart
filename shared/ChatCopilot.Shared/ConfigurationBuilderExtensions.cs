using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Configuration;
using System.Reflection;

namespace ChatCopilot.Shared;

internal static class ConfigurationBuilderExtensions
{
    private const string AspnetEnvVar = "ASPNETCORE_ENVIRONMENT";

    public static void AddKMConfigurationSources(
        this IConfigurationBuilder builder,
        bool useAppSettingsFiles = true,
        bool useEnvVars = true,
        bool useSecretManager = true,
        string? settingsDirectory = null)
    {
        string env = Environment.GetEnvironmentVariable(AspnetEnvVar) ?? string.Empty;

        settingsDirectory ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

        builder.SetBasePath(settingsDirectory);

        if (useAppSettingsFiles)
        {
            string main = @"D:\appsettings\chat-copilot.json";

            if (!File.Exists(main))
            {
                throw new ConfigurationException($"appsettings.json not found. Directory: {settingsDirectory}");
            }

            builder.AddJsonFile(main, optional: false);

            if (env.Equals("development", StringComparison.OrdinalIgnoreCase))
            {
                string file1 = Path.Combine(settingsDirectory, "appsettings.development.json");
                string file2 = Path.Combine(settingsDirectory, "appsettings.Development.json");

                if (File.Exists(file1))
                {
                    builder.AddJsonFile(file1, optional: false);
                }
                else if (File.Exists(file2))
                {
                    builder.AddJsonFile(file2, optional: false);
                }
            }

            if (env.Equals("production", StringComparison.OrdinalIgnoreCase))
            {
                string file1 = Path.Combine(settingsDirectory, "appsettings.production.json");
                string file2 = Path.Combine(settingsDirectory, "appsettings.Production.json");

                if (File.Exists(file1))
                {
                    builder.AddJsonFile(file1, optional: false);
                }
                else if (File.Exists(file2))
                {
                    builder.AddJsonFile(file2, optional: false);
                }
            }
        }

        if (useSecretManager)
        {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null && env.Equals("development", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddUserSecrets(entryAssembly, optional: true);
            }
        }

        if (useEnvVars)
        {
            builder.AddEnvironmentVariables();
        }
    }
}
