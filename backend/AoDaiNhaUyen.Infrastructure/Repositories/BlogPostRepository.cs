using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class BlogPostRepository(AppDbContext dbContext) : IBlogPostRepository
{
  public async Task<(IReadOnlyList<BlogPost> Items, int TotalCount)> GetAllAsync(
    BlogPostStatus? status,
    string? tag,
    string? categorySlug,
    string? search,
    int page,
    int pageSize,
    bool includeDeleted = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery(includeDeleted);

    if (status.HasValue)
    {
      query = query.Where(p => p.Status == status.Value);
    }

    if (!string.IsNullOrWhiteSpace(tag))
    {
      var tagPattern = $"%\"{tag.Trim()}\"%";
      query = query.Where(p => EF.Functions.Like(p.Tags, tagPattern));
    }

    if (!string.IsNullOrWhiteSpace(categorySlug))
    {
      var normalizedCategorySlug = categorySlug.Trim();
      query = query.Where(p => p.BlogCategory != null && p.BlogCategory.Slug == normalizedCategorySlug);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      var pattern = $"%{search.Trim()}%";
      query = query.Where(p => EF.Functions.ILike(p.Title, pattern) || EF.Functions.ILike(p.Excerpt, pattern));
    }

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
      .OrderByDescending(p => p.PublishedAt ?? p.UpdatedAt)
      .ThenByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    return (items, totalCount);
  }

  public async Task<BlogPost?> GetBySlugAsync(string slug, bool includeDrafts = false, CancellationToken cancellationToken = default)
  {
    var query = BaseQuery(false).Where(p => p.Slug == slug);
    if (!includeDrafts) query = query.Where(p => p.Status == BlogPostStatus.Published);
    return await query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<BlogPost?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
  {
    return await BaseQuery(includeDeleted).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
  }

  public async Task<IReadOnlyList<BlogPost>> GetRelatedAsync(Guid postId, IReadOnlyList<string> tags, int count, CancellationToken cancellationToken = default)
  {
    if (tags.Count == 0) return [];

    var candidates = await BaseQuery(false)
      .Where(p => p.Id != postId && p.Status == BlogPostStatus.Published)
      .OrderByDescending(p => p.PublishedAt ?? p.UpdatedAt)
      .ThenByDescending(p => p.CreatedAt)
      .Take(50)
      .ToListAsync(cancellationToken);

    var tagSet = tags.Select(t => t.Trim()).Where(t => t.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
    return candidates
      .Where(post => ParseTags(post.Tags).Any(tagSet.Contains))
      .Take(count)
      .ToList();
  }

  public async Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
  {
    var tagsJson = await dbContext.BlogPosts
      .AsNoTracking()
      .Where(p => p.Status == BlogPostStatus.Published)
      .Select(p => p.Tags)
      .ToListAsync(cancellationToken);

    return tagsJson
      .SelectMany(ParseTags)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(x => x)
      .ToList();
  }

  public async Task AddAsync(BlogPost post, CancellationToken cancellationToken = default)
  {
    dbContext.BlogPosts.Add(post);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default)
  {
    dbContext.BlogPosts.Update(post);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var post = await dbContext.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
      ?? throw new InvalidOperationException("Không tìm thấy bài viết.");

    post.IsDeleted = true;
    post.IsActive = false;
    post.DeletedAt = DateTime.UtcNow;
    post.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private IQueryable<BlogPost> BaseQuery(bool includeDeleted)
  {
    var query = dbContext.BlogPosts.AsNoTracking().Include(p => p.Author).Include(p => p.BlogCategory).AsQueryable();
    return includeDeleted ? query.IgnoreQueryFilters() : query;
  }

  private static IEnumerable<string> ParseTags(string json)
  {
    try
    {
      return System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
    }
    catch
    {
      return [];
    }
  }
}
