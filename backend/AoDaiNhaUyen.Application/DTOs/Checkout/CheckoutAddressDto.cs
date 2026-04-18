namespace AoDaiNhaUyen.Application.DTOs.Checkout;

public sealed record CheckoutAddressDto(
  string RecipientName,
  string RecipientPhone,
  string Province,
  string District,
  string? Ward,
  string AddressLine);
