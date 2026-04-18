namespace AoDaiNhaUyen.Application.DTOs.Checkout;

public sealed record CheckoutResultDto(
  long OrderId,
  string OrderCode,
  string OrderStatus,
  string PaymentStatus,
  decimal Subtotal,
  decimal DiscountAmount,
  decimal ShippingFee,
  decimal TotalAmount,
  DateTime PlacedAt);
