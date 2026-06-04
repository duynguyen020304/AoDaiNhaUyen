namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ProductVariantDto(
  Guid Id,
  string Sku,
  string? VariantName,
  string? Size,
  string? Color,
  decimal Price,
  decimal? SalePrice,
  int StockQty,
  bool IsDefault,
  string Status);
