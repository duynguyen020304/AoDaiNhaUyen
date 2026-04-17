using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Application.Services;

public sealed class CatalogService(
  ICategoryRepository categoryRepository,
  IProductRepository productRepository) : ICatalogService
{
  public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var categories = await categoryRepository.GetActiveAsync(cancellationToken);

    return categories
      .Select(c => new CategoryDto(
        c.Id,
        c.Parent,
        c.Name,
        c.Slug,
        c.Description,
        c.ImageUrl,
        c.SortOrder,
        c.IsActive))
      .ToList();
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

      var primaryImage = p.Images
        .OrderBy(i => i.SortOrder)
        .FirstOrDefault(i => i.IsPrimary)?.ImageUrl ??
        p.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl;

      return new ProductListItemDto(
        p.Id,
        p.Name,
        p.Slug,
        p.ProductType,
        p.Status,
        p.ShortDescription,
        primaryVariant?.Price ?? 0,
        primaryVariant?.SalePrice,
        p.Category.Slug,
        p.IsFeatured,
        primaryVariant?.StockQty ?? 0,
        primaryImage,
        primaryVariant?.Sku);
    }).ToList();

    return new PagedResult<ProductListItemDto>(mapped, totalCount, validatedPage, validatedPageSize);
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
      .Select(i => new ProductImageDto(i.ImageUrl, i.AltText, i.SortOrder, i.IsPrimary))
      .ToList();

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
      images);
  }
}
