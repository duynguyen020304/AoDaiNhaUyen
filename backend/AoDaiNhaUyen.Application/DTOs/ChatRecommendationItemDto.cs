namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatRecommendationItemDto(
  Guid ProductId,
  string Name,
  string CategorySlug,
  string ProductType,
  decimal Price,
  decimal? SalePrice,
  string? PrimaryImageUrl,
  Guid? PrimaryVariantId,
  string Rationale);
