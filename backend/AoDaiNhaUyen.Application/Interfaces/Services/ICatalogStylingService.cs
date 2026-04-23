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
    IReadOnlyList<long>? excludeProductIds = null,
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
    IReadOnlyList<long> productIds,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<long>> ResolveProductReferencesAsync(
    string message,
    IReadOnlyList<long> shortlistedProductIds,
    CancellationToken cancellationToken = default);
}
