using ChatCopilot.Shared.Ocr.Tesseract;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats;

namespace ChatCopilot.Shared.Ocr;

public static class ConfigurationExtensions
{
    private const string ConfigOcrType = "ImageOcrType";

    public static IOcrEngine? CreateCustomOcr(this IConfiguration configuration)
    {
        string ocrType = configuration.GetSection($"{MemoryConfiguration.KernelMemorySection}:{ConfigOcrType}").Value ?? string.Empty;

        switch (ocrType)
        {
            case string x when x.Equals(TesseractOptions.SectionName, StringComparison.OrdinalIgnoreCase):

                TesseractOptions? tesseractOptions = configuration.GetSection($"{MemoryConfiguration.KernelMemorySection}:{MemoryConfiguration.ServicesSection}:{TesseractOptions.SectionName}")
                    .Get<TesseractOptions>();

                if (tesseractOptions == null)
                {
                    throw new ConfigurationException($"Missing configuration for {ConfigOcrType}: {ocrType}");
                }

                return new TesseractOcrEngine(tesseractOptions);
            default:
                break;
        }

        return null;
    }
}
