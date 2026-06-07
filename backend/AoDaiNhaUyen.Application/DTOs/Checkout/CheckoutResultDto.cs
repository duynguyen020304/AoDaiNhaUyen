namespace AoDaiNhaUyen.Application.DTOs.Checkout;

public sealed record CheckoutResultDto(
  Guid OrderId,
  string OrderCode,
  string OrderStatus,
  string PaymentStatus,
  decimal Subtotal,
  decimal DiscountAmount,
  decimal ShippingFee,
  decimal TotalAmount,
  DateTime PlacedAt,
  string? AppliedPromoCode,
  string? DiscountLabel);
