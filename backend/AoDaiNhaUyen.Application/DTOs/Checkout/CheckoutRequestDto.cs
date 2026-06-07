namespace AoDaiNhaUyen.Application.DTOs.Checkout;

public sealed record CheckoutRequestDto(
  Guid? AddressId,
  CheckoutAddressDto? Address,
  string? Note,
  string PaymentMethod,
  string? PromoCode);
