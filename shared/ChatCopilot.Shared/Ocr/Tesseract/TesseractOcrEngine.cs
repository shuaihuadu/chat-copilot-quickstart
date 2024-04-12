using Microsoft.KernelMemory.DataFormats;
using Tesseract;

namespace ChatCopilot.Shared.Ocr.Tesseract;

public class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine _engine;

    public TesseractOcrEngine(TesseractOptions tesseractOptions)
    {
        this._engine = new TesseractEngine(tesseractOptions.FilePath, tesseractOptions.Language);
    }

    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        await using (MemoryStream imageStream = new MemoryStream())
        {
            await imageContent.CopyToAsync(imageStream);
            imageStream.Position = 0;

            using Pix? image = Pix.LoadFromMemory(imageStream.ToArray());

            using Page? page = this._engine.Process(image);

            return page.GetText();
        }
    }
}
