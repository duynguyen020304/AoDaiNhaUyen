namespace AoDaiNhaUyen.Application.DTOs.Cart;

public sealed record AddCartItemDto(
  long VariantId,
  int Quantity);
