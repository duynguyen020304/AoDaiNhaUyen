namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CatalogAiTryOnRequestDto(
  string? LegacyGarmentId,
  byte[] PersonImageBytes,
  string PersonImageMimeType,
  Guid? GarmentProductId,
  Guid? GarmentVariantId,
  IReadOnlyList<Guid> AccessoryProductIds,
  byte[]? LegacyGarmentImageBytes,
  string? LegacyGarmentImageMimeType,
  IReadOnlyList<AiTryOnAccessoryImageDto> LegacyAccessoryImages,
  Guid? UserId = null,
  string? GuestKeyHash = null);
