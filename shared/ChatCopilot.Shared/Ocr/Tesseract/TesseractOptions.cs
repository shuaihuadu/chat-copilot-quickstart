using System.ComponentModel.DataAnnotations;

namespace ChatCopilot.Shared.Ocr.Tesseract;

public class TesseractOptions
{
    public const string SectionName = "Tesseract";

    [Required]
    public string? FilePath { get; set; } = string.Empty;

    [Required]
    public string? Language { get; set; } = string.Empty;
}
