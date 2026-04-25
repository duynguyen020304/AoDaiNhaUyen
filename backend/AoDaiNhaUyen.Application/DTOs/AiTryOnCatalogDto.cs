namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnCatalogDto(
  AiTryOnCatalogPageDto Garments,
  AiTryOnCatalogPageDto Accessories,
  IReadOnlyList<AiTryOnCatalogCategoryDto> GarmentCategories,
  IReadOnlyList<AiTryOnCatalogCategoryDto> AccessoryCategories);

public sealed record AiTryOnCatalogPageDto(
  IReadOnlyList<AiTryOnCatalogItemDto> Items,
  int Page,
  int PageSize,
  int TotalItems,
  int TotalPages);

public sealed record AiTryOnCatalogCategoryDto(
  string Key,
  string Label);

public sealed record AiTryOnCatalogQueryDto(
  int GarmentPage = 1,
  int AccessoryPage = 1,
  int PageSize = 6,
  string? GarmentCategory = null,
  string? AccessoryCategory = null);
