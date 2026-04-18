namespace AoDaiNhaUyen.Application.DTOs.Cart;

public sealed record CartDto(
  long Id,
  long UserId,
  int TotalItemCount,
  decimal Subtotal,
  IReadOnlyList<CartItemDto> Items);
