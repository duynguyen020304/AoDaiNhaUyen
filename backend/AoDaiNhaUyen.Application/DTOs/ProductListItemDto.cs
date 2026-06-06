namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ProductListItemDto(
  Guid Id,
  string Name,
  string Slug,
  string ProductType,
  string Status,
  string? ShortDescription,
  decimal Price,
  decimal? SalePrice,
  string CategorySlug,
  bool IsFeatured,
  int StockQty,
  string? PrimaryImageUrl,
  Guid? PrimaryVariantId,
  string? PrimaryVariantSku,
  double AverageRating,
  int TotalReviews);
