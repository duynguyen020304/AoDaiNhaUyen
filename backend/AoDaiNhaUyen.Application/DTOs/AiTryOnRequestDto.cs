namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnRequestDto(
  string GarmentId,
  byte[] PersonImageBytes,
  string PersonImageMimeType,
  byte[] GarmentImageBytes,
  string GarmentImageMimeType,
  IReadOnlyList<AiTryOnAccessoryImageDto> AccessoryImages);
