namespace AoDaiNhaUyen.Application.DTOs.Cart;

public sealed record AddCartItemDto(
  Guid VariantId,
  int Quantity);
