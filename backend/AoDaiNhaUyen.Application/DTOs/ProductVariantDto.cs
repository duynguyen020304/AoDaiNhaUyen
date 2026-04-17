namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ProductVariantDto(
  long Id,
  string Sku,
  string? VariantName,
  string? Size,
  string? Color,
  decimal Price,
  decimal? SalePrice,
  int StockQty,
  bool IsDefault,
  string Status);
