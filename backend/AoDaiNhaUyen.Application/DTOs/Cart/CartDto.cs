namespace AoDaiNhaUyen.Application.DTOs.Cart;

public sealed record CartDto(
  Guid Id,
  Guid UserId,
  int TotalItemCount,
  decimal Subtotal,
  IReadOnlyList<CartItemDto> Items);
