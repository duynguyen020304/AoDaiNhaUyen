namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CatalogAiTryOnRequestDto(
  string? LegacyGarmentId,
  byte[] PersonImageBytes,
  string PersonImageMimeType,
  long? GarmentProductId,
  long? GarmentVariantId,
  IReadOnlyList<long> AccessoryProductIds,
  byte[]? LegacyGarmentImageBytes,
  string? LegacyGarmentImageMimeType,
  IReadOnlyList<AiTryOnAccessoryImageDto> LegacyAccessoryImages);
