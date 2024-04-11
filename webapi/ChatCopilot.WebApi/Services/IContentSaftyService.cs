namespace ChatCopilot.WebApi.Services;

public interface IContentSaftyService : IDisposable
{
    Task<ImageAnalysisResponse> ImageAnalysisAsync(IFormFile formFile, CancellationToken cancellationToken);

    List<string> ParseViolatedCatagories(ImageAnalysisResponse imageAnalysisResponse, short threshold);
}