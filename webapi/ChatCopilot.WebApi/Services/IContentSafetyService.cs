namespace ChatCopilot.WebApi.Services;

public interface IContentSafetyService : IDisposable
{
    Task<ImageAnalysisResponse> ImageAnalysisAsync(IFormFile formFile, CancellationToken cancellationToken);

    List<string> ParseViolatedCatagories(ImageAnalysisResponse imageAnalysisResponse, short threshold);
}