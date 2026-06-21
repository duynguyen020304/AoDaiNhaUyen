using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Collections;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed partial class AdminCollectionService(AppDbContext dbContext) : IAdminCollectionService
{
  public async Task<PagedResult<CollectionListItemDto>> GetListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken ct = default)
  {
    var query = dbContext.Collections.AsNoTracking().AsQueryable();
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = $"%{search.Trim()}%";
      query = query.Where(x => EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Slug, term));
    }

    var total = await query.CountAsync(ct);
    var items = await query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreatedAt)
      .Skip((page - 1) * pageSize).Take(pageSize)
      .Select(x => new CollectionListItemDto(x.Id, x.Name, x.Slug, x.Description, x.CoverImageUrl, x.IsPublished, x.IsFeatured, x.SortOrder, x.Products.Count(p => !p.IsDeleted), x.PublishedAt, x.CreatedAt, x.UpdatedAt, x.IsDeleted))
      .ToListAsync(ct);
    return new PagedResult<CollectionListItemDto>(items, total, page, pageSize);
  }

  public Task<CollectionDetailDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
  {
    var query = dbContext.Collections.AsNoTracking().Where(x => x.Id == id);
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    return ProjectDetail(query).FirstOrDefaultAsync(ct);
  }

  public async Task<CollectionDetailDto> CreateAsync(CreateCollectionRequest request, CancellationToken ct = default)
  {
    var name = Required(request.Name, "Tên collection là bắt buộc.", 200);
    var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(name) : Slugify(request.Slug);
    if (await dbContext.Collections.AnyAsync(x => x.Slug == slug, ct)) throw new ArgumentException("Slug collection đã tồn tại.");
    var now = DateTime.UtcNow;
    var entity = new Collection
    {
      Id = Guid.NewGuid(), Name = name, Slug = slug, Description = Trim(request.Description, 2000), CoverImageUrl = Trim(request.CoverImageUrl, 1000),
      IsPublished = request.IsPublished, IsFeatured = request.IsFeatured, SortOrder = request.SortOrder, PublishedAt = request.IsPublished ? now : null, CreatedAt = now, UpdatedAt = now
    };
    dbContext.Collections.Add(entity);
    await dbContext.SaveChangesAsync(ct);
    return (await GetByIdAsync(entity.Id, true, ct))!;
  }

  public async Task<CollectionDetailDto?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken ct = default)
  {
    var entity = await dbContext.Collections.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
    if (entity is null || entity.IsDeleted) return null;
    var name = Required(request.Name, "Tên collection là bắt buộc.", 200);
    var slug = string.IsNullOrWhiteSpace(request.Slug) ? entity.Slug : Slugify(request.Slug);
    if (!slug.Equals(entity.Slug, StringComparison.OrdinalIgnoreCase) && await dbContext.Collections.AnyAsync(x => x.Slug == slug && x.Id != id, ct)) throw new ArgumentException("Slug collection đã tồn tại.");
    entity.Name = name; entity.Slug = slug; entity.Description = Trim(request.Description, 2000); entity.CoverImageUrl = Trim(request.CoverImageUrl, 1000);
    entity.IsFeatured = request.IsFeatured; entity.SortOrder = request.SortOrder;
    if (!entity.IsPublished && request.IsPublished) entity.PublishedAt = DateTime.UtcNow;
    if (!request.IsPublished) entity.PublishedAt = null;
    entity.IsPublished = request.IsPublished; entity.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return await GetByIdAsync(id, true, ct);
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
  {
    var entity = await dbContext.Collections.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
    if (entity is null || entity.IsDeleted) return false;
    entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct); return true;
  }

  public async Task<bool> RestoreAsync(Guid id, CancellationToken ct = default)
  {
    var entity = await dbContext.Collections.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
    if (entity is null || !entity.IsDeleted) return false;
    entity.IsDeleted = false; entity.DeletedAt = null; entity.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct); return true;
  }

  public async Task<CollectionDetailDto?> AddProductAsync(Guid id, AddProductToCollectionRequest request, CancellationToken ct = default)
  {
    var collection = await dbContext.Collections.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    if (collection is null) return null;
    var productExists = await dbContext.Products.AnyAsync(x => x.Id == request.ProductId && !x.IsDeleted, ct);
    if (!productExists) throw new ArgumentException("Không tìm thấy sản phẩm.");
    var existing = await dbContext.CollectionProducts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CollectionId == id && x.ProductId == request.ProductId, ct);
    if (existing is null) dbContext.CollectionProducts.Add(new CollectionProduct { Id = Guid.NewGuid(), CollectionId = id, ProductId = request.ProductId, SortOrder = request.SortOrder, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    else { existing.IsDeleted = false; existing.DeletedAt = null; existing.SortOrder = request.SortOrder; existing.UpdatedAt = DateTime.UtcNow; }
    collection.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return await GetByIdAsync(id, true, ct);
  }

  public async Task<CollectionDetailDto?> RemoveProductAsync(Guid id, Guid productId, CancellationToken ct = default)
  {
    var row = await dbContext.CollectionProducts.FirstOrDefaultAsync(x => x.CollectionId == id && x.ProductId == productId && !x.IsDeleted, ct);
    if (row is null) return null;
    row.IsDeleted = true; row.DeletedAt = DateTime.UtcNow; row.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return await GetByIdAsync(id, true, ct);
  }

  private static IQueryable<CollectionDetailDto> ProjectDetail(IQueryable<Collection> query) =>
    query.Select(x => new CollectionDetailDto(x.Id, x.Name, x.Slug, x.Description, x.CoverImageUrl, x.IsPublished, x.IsFeatured, x.SortOrder, x.PublishedAt, x.CreatedAt, x.UpdatedAt, x.IsDeleted,
      x.Products.Where(cp => !cp.IsDeleted && !cp.Product.IsDeleted).OrderBy(cp => cp.SortOrder).Select(cp => new CollectionProductDto(cp.Id, cp.ProductId, cp.Product.Name, cp.Product.Slug, cp.Product.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(), cp.SortOrder)).ToList()));

  private static string Required(string value, string message, int max) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : Trim(value, max)!;
  private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
  private static string Slugify(string text)
  {
    var normalized = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
    var ascii = new string(chars).Normalize(NormalizationForm.FormC);
    ascii = SlugRegex().Replace(ascii, "-").Trim('-');
    return string.IsNullOrWhiteSpace(ascii) ? Guid.NewGuid().ToString("N") : ascii;
  }
  [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)] private static partial Regex SlugRegex();
}
