using AoDaiNhaUyen.Application.DTOs.BlogPost;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IBlogImageVisibilityService
{
  Task<string> ResolveUrlAsync(string objectKey, bool isPublic, string? publicObjectKey, CancellationToken ct = default);
  Task<BlogImageVisibilityDto> MakePublicAsync(Guid blogImageId, Guid? blogPostId = null, CancellationToken ct = default);
  Task<BlogImageVisibilityDto> MakePrivateAsync(Guid blogImageId, Guid? blogPostId = null, CancellationToken ct = default);
}
