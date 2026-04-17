using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICatalogService
{
  Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<CategoryTreeDto>> GetHeaderCategoriesAsync(CancellationToken cancellationToken = default);
  Task<PagedResult<ProductListItemDto>> GetProductsAsync(
    string? categorySlug,
    string? productType,
    bool? featured,
    string? size,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
