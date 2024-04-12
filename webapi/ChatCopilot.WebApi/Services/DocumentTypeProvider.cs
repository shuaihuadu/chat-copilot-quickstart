namespace ChatCopilot.WebApi.Services;

public class DocumentTypeProvider
{

    private readonly Dictionary<string, bool> supportedTypes;

    public DocumentTypeProvider(bool allowImageOcr)
    {
        this.supportedTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { FileExtensions.MarkDown, false},
            { FileExtensions.MsWord, false},
            { FileExtensions.MsWordX, false},
            { FileExtensions.Pdf, false},
            { FileExtensions.PlainText, false},
            { FileExtensions.ImageBmp, true },
            { FileExtensions.ImageGif, true },
            { FileExtensions.ImagePng, true },
            { FileExtensions.ImageJpg, true },
            { FileExtensions.ImageJpeg, true },
            { FileExtensions.ImageTiff, true }
        };
    }

    public bool IsSupported(string extension, out bool isSafetyTarget)
    {
        return this.supportedTypes.TryGetValue(extension, out isSafetyTarget);
    }
}