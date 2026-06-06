using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Application.Services;

public sealed class CatalogService(
  ICategoryRepository categoryRepository,
  IProductRepository productRepository,
  IImageVisibilityService imageVisibilityService,
  ICommentService commentService) : ICatalogService
{
  public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var categories = await categoryRepository.GetActiveAsync(cancellationToken);

    var result = new List<CategoryDto>(categories.Count);
    foreach (var c in categories)
    {
      var imageUrl = c.ImageUrl;
      if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
      {
        imageUrl = await imageVisibilityService.ResolveUrlAsync(imageUrl, false, null, cancellationToken);
      }

      result.Add(new CategoryDto(
        c.Id,
        c.Parent,
        c.Name,
        c.Slug,
        c.Description,
        imageUrl,
        c.SortOrder,
        c.IsActive));
    }

    return result;
  }

  public async Task<IReadOnlyList<CategoryTreeDto>> GetHeaderCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var categories = await categoryRepository.GetActiveAsync(cancellationToken);
    var childrenByParent = categories
      .Where(c => c.Parent.HasValue)
      .GroupBy(c => c.Parent!.Value)
      .ToDictionary(
        g => g.Key,
        g => g
          .OrderBy(c => c.SortOrder)
          .ThenBy(c => c.Name)
          .Select(c => new CategoryTreeChildDto(c.Id, c.Name, c.Slug, c.SortOrder))
          .ToList() as IReadOnlyList<CategoryTreeChildDto>);

    return categories
      .Where(c => c.Parent is null)
      .OrderBy(c => c.SortOrder)
      .ThenBy(c => c.Name)
      .Select(c => new CategoryTreeDto(
        c.Id,
        c.Name,
        c.Slug,
        c.SortOrder,
        childrenByParent.TryGetValue(c.Id, out var children) ? children : []))
      .ToList();
  }

  public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
    string? categorySlug,
    string? productType,
    bool? featured,
    string? size,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var validatedPage = page <= 0 ? 1 : page;
    var validatedPageSize = pageSize is <= 0 or > 100 ? 12 : pageSize;

    var (items, totalCount) = await productRepository.GetPagedAsync(
      categorySlug,
      productType,
      featured,
      size,
      validatedPage,
      validatedPageSize,
      cancellationToken);

    var mapped = items.Select(p =>
    {
      var normalizedSize = size?.Trim();
      var primaryVariant = p.Variants
        .Where(v => string.IsNullOrWhiteSpace(normalizedSize) ||
          string.Equals(v.Size, normalizedSize, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(v => v.IsDefault)
        .ThenBy(v => v.Id)
        .FirstOrDefault() ?? p.Variants
          .OrderByDescending(v => v.IsDefault)
          .ThenBy(v => v.Id)
          .FirstOrDefault();

      var primaryImageEntity = p.Images
        .OrderBy(i => i.SortOrder)
        .FirstOrDefault(i => i.IsPrimary) ??
        p.Images.OrderBy(i => i.SortOrder).FirstOrDefault();

      var primaryImage = primaryImageEntity?.ImageUrl;

      return new { Entity = p, PrimaryVariant = primaryVariant, PrimaryImageEntity = primaryImageEntity, PrimaryImage = primaryImage };
    }).ToList();

    // Resolve image URLs via visibility service
    var resolvedItems = new List<ProductListItemDto>();
    foreach (var p in mapped)
    {
      var resolvedUrl = p.PrimaryImage is not null
        ? await imageVisibilityService.ResolveUrlAsync(
            p.PrimaryImage,
            p.PrimaryImageEntity?.IsPublic ?? false,
            p.PrimaryImageEntity?.PublicObjectKey,
            cancellationToken)
        : null;

      resolvedItems.Add(new ProductListItemDto(
        p.Entity.Id,
        p.Entity.Name,
        p.Entity.Slug,
        p.Entity.ProductType,
        p.Entity.Status,
        p.Entity.ShortDescription,
        p.PrimaryVariant?.Price ?? 0,
        p.PrimaryVariant?.SalePrice,
        p.Entity.Category.Slug,
        p.Entity.IsFeatured,
        p.PrimaryVariant?.StockQty ?? 0,
        resolvedUrl,
        p.PrimaryVariant?.Id,
        p.PrimaryVariant?.Sku,
        0,
        0));
    }

    // Batch-fetch review summaries in one DB round-trip
    var productIds = resolvedItems.Select(p => p.Id).Distinct().ToList();
    var summaries = await commentService.GetReviewSummariesAsync(productIds, cancellationToken);

    var enrichedItems = resolvedItems.Select(p =>
    {
      var summary = summaries.GetValueOrDefault(p.Id);
      return summary is not null
        ? new ProductListItemDto(
            p.Id, p.Name, p.Slug, p.ProductType, p.Status,
            p.ShortDescription, p.Price, p.SalePrice, p.CategorySlug,
            p.IsFeatured, p.StockQty, p.PrimaryImageUrl,
            p.PrimaryVariantId, p.PrimaryVariantSku,
            summary.AverageRating,
            summary.TotalReviews)
        : p;
    }).ToList();

    return new PagedResult<ProductListItemDto>(enrichedItems, totalCount, validatedPage, validatedPageSize);
  }

  public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    var product = await productRepository.GetBySlugAsync(slug, cancellationToken);
    if (product is null)
    {
      return null;
    }

    var images = product.Images
      .OrderBy(i => i.SortOrder)
      .Select(i => new { Entity = i })
      .ToList();

    var resolvedImages = new List<ProductImageDto>();
    foreach (var img in images)
    {
      var resolvedUrl = await imageVisibilityService.ResolveUrlAsync(
        img.Entity.ImageUrl,
        img.Entity.IsPublic,
        img.Entity.PublicObjectKey,
        cancellationToken);
      resolvedImages.Add(new ProductImageDto(resolvedUrl, img.Entity.AltText, img.Entity.SortOrder, img.Entity.IsPrimary));
    }

    var variants = product.Variants
      .OrderByDescending(v => v.IsDefault)
      .ThenBy(v => v.Id)
      .Select(v => new ProductVariantDto(
        v.Id,
        v.Sku,
        v.VariantName,
        v.Size,
        v.Color,
        v.Price,
        v.SalePrice,
        v.StockQty,
        v.IsDefault,
        v.Status))
      .ToList();

    return new ProductDetailDto(
      product.Id,
      product.Name,
      product.Slug,
      product.ProductType,
      product.Status,
      product.ShortDescription,
      product.Description,
      product.Material,
      product.Brand,
      product.Origin,
      product.CareInstruction,
      product.Category.Name,
      product.Category.Slug,
      product.IsFeatured,
      product.CreatedAt,
      product.UpdatedAt,
      variants,
      resolvedImages,
      await MapReviewSummaryAsync(product.Id, cancellationToken));
  }

  private async Task<ReviewSummaryDto?> MapReviewSummaryAsync(
    Guid productId,
    CancellationToken cancellationToken)
  {
    return await commentService.GetReviewSummaryAsync(productId, cancellationToken);
  }
}
