using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
  public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
    string? categorySlug,
    string? productType,
    bool? featured,
    string? size,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Products
      .AsNoTracking()
      .Include(p => p.Category)
      .Include(p => p.Variants)
      .Include(p => p.Images)
      .Where(p => p.Status == "active")
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(categorySlug))
    {
      query = query.Where(p => p.Category.Slug == categorySlug);
    }

    if (!string.IsNullOrWhiteSpace(productType))
    {
      query = query.Where(p => p.ProductType == productType);
    }

    if (featured.HasValue)
    {
      query = query.Where(p => p.IsFeatured == featured.Value);
    }

    if (!string.IsNullOrWhiteSpace(size))
    {
      var normalizedSize = size.Trim();
      query = query.Where(p => p.Variants.Any(v =>
        v.Status == "active" &&
        v.Size != null &&
        v.Size.ToLower() == normalizedSize.ToLower()));
    }

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
      .OrderByDescending(p => p.IsFeatured)
      .ThenByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    return (items, totalCount);
  }

  public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    return await dbContext.Products
      .AsNoTracking()
      .Include(p => p.Category)
      .Include(p => p.Variants)
      .Include(p => p.Images)
      .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == "active", cancellationToken);
  }
}
