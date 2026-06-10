using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class BlogCategoryRepository(AppDbContext dbContext) : IBlogCategoryRepository
{
  public async Task<IReadOnlyList<BlogCategory>> GetPublicAsync(CancellationToken cancellationToken = default)
    => await dbContext.BlogCategories
      .AsNoTracking()
      .Include(category => category.Posts.Where(post => post.Status == BlogPostStatus.Published && !post.IsDeleted))
      .Where(category => category.IsActive)
      .OrderBy(category => category.SortOrder)
      .ThenBy(category => category.Name)
      .ToListAsync(cancellationToken);

  public async Task<BlogCategory?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    => await dbContext.BlogCategories
      .AsNoTracking()
      .FirstOrDefaultAsync(category => category.Slug == slug && category.IsActive, cancellationToken);

  public async Task<BlogCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    => await dbContext.BlogCategories
      .AsNoTracking()
      .FirstOrDefaultAsync(category => category.Id == id && category.IsActive, cancellationToken);
}
