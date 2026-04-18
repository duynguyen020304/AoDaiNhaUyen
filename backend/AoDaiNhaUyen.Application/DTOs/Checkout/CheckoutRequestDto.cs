namespace AoDaiNhaUyen.Application.DTOs.Checkout;

public sealed record CheckoutRequestDto(
  long? AddressId,
  CheckoutAddressDto? Address,
  string? Note,
  string PaymentMethod);
