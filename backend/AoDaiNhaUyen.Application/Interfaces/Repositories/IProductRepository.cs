using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface IProductRepository
{
  Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
    string? categorySlug,
    string? productType,
    bool? featured,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
