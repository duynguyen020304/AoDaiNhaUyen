using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IBlogPostService
{
  Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(BlogPostStatus? status, string? tag, string? categorySlug, string? search, int page, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default);
  Task<BlogPostDto?> GetBySlugAsync(string slug, bool includeDrafts = false, CancellationToken cancellationToken = default);
  Task<BlogPostDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BlogPostListItemDto>> GetRelatedAsync(string slug, int count, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default);
  Task<BlogPostDto> CreateAsync(CreateBlogPostRequest request, CancellationToken cancellationToken = default);
  Task<BlogPostDto> UpdateAsync(Guid id, UpdateBlogPostRequest request, CancellationToken cancellationToken = default);
  Task<BlogPostDto> UpdateSeoAsync(Guid id, UpdateBlogPostSeoRequest request, CancellationToken cancellationToken = default);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
  Task<string> BuildBlogSitemapAsync(string siteBaseUrl, CancellationToken cancellationToken = default);
  Task<string> BuildLlmsTextAsync(string siteBaseUrl, CancellationToken cancellationToken = default);
}
