namespace AoDaiNhaUyen.Application.DTOs.Cart;

public sealed record CartItemDto(
  long Id,
  long VariantId,
  long ProductId,
  string ProductName,
  string ProductSlug,
  string? Sku,
  string? VariantName,
  string? Size,
  string? Color,
  string? ImageUrl,
  decimal Price,
  decimal? SalePrice,
  int Quantity,
  decimal LineTotal);
