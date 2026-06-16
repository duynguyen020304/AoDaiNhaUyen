using AoDaiNhaUyen.Application.DTOs.BlogPost;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Generates and validates AI-assisted blog drafts.</summary>
public interface IBlogAiDraftService
{
  Task<GeneratedBlogDraftResponse> GenerateDraftAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken = default);
}
