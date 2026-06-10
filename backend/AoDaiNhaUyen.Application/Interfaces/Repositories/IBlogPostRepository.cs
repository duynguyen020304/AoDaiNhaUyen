using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface IBlogPostRepository
{
  Task<(IReadOnlyList<BlogPost> Items, int TotalCount)> GetAllAsync(
    BlogPostStatus? status,
    string? tag,
    string? categorySlug,
    string? search,
    int page,
    int pageSize,
    bool includeDeleted = false,
    CancellationToken cancellationToken = default);

  Task<BlogPost?> GetBySlugAsync(string slug, bool includeDrafts = false, CancellationToken cancellationToken = default);
  Task<BlogPost?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BlogPost>> GetRelatedAsync(Guid postId, IReadOnlyList<string> tags, int count, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default);
  Task AddAsync(BlogPost post, CancellationToken cancellationToken = default);
  Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default);
  Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
