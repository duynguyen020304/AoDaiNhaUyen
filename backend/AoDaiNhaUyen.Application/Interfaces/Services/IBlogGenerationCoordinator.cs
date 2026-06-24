using AoDaiNhaUyen.Application.DTOs.BlogPost;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Coordinates multi-phase AI blog generation before editor handoff.</summary>
public interface IBlogGenerationCoordinator
{
  Task<BlogGenerationProgressResponse> GenerateAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken = default);
}
