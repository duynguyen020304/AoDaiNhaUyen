namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatRecommendationItemDto(
  long ProductId,
  string Name,
  string CategorySlug,
  decimal Price,
  decimal? SalePrice,
  string? PrimaryImageUrl,
  long? PrimaryVariantId,
  string Rationale);
