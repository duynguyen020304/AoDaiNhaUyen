namespace AoDaiNhaUyen.Application.DTOs;

/// <summary>
/// Cảnh báo sản phẩm sắp hết hàng.
/// </summary>
public sealed record LowStockAlertDto(
  Guid VariantId,
  string ProductName,
  string? VariantName,
  string? Size,
  string? Color,
  string Sku,
  int StockQty);
