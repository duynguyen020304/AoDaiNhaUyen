using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICatalogStylingService
{
  Task<IReadOnlyList<ChatRecommendationItemDto>> RecommendAsync(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    int limit,
    IReadOnlyList<Guid>? excludeProductIds = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ChatRecommendationItemDto>> LookupAsync(
    string query,
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    int limit,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ChatRecommendationItemDto>> CompareAsync(
    IReadOnlyList<Guid> productIds,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Guid>> ResolveProductReferencesAsync(
    string message,
    IReadOnlyList<Guid> shortlistedProductIds,
    CancellationToken cancellationToken = default);
}
