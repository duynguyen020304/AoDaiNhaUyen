namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnCatalogItemDto(
  Guid ProductId,
  Guid? DefaultVariantId,
  string Name,
  string ProductType,
  string CategorySlug,
  string ThumbnailUrl,
  string AiAssetUrl,
  bool IsFeatured);
