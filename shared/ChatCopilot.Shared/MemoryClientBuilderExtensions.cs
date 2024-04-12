using ChatCopilot.Shared.Ocr;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats;

namespace ChatCopilot.Shared;

public static class MemoryClientBuilderExtensions
{
    public static IKernelMemoryBuilder WithCustomOcr(this IKernelMemoryBuilder builder, IConfiguration configuration)
    {
        IOcrEngine? ocrEngine = configuration.CreateCustomOcr();

        if (ocrEngine is not null)
        {
            builder.WithCustomImageOcr(ocrEngine);
        }

        return builder;
    }
}
