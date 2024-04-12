using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

namespace ChatCopilot.Shared;

public static class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder FromAppSettings(this IKernelMemoryBuilder builder, string? settingsDirectory = null)
    {
        return new ServiceConfiguration(settingsDirectory).PrepareBuilder(builder);
    }

    public static IKernelMemoryBuilder FromMemoryConfiguration(this IKernelMemoryBuilder builder, KernelMemoryConfig kernelMemoryConfig, IConfiguration configuration)
    {
        return new ServiceConfiguration(configuration, kernelMemoryConfig).PrepareBuilder(builder);
    }
}
