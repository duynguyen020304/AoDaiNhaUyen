namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnCatalogItemDto(
  long ProductId,
  long? DefaultVariantId,
  string Name,
  string ProductType,
  string CategorySlug,
  string ThumbnailUrl,
  string AiAssetUrl,
  bool IsFeatured);
