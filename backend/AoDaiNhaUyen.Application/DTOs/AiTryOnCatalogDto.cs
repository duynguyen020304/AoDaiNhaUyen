namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnCatalogDto(
  IReadOnlyList<AiTryOnCatalogItemDto> Garments,
  IReadOnlyList<AiTryOnCatalogItemDto> Accessories);
