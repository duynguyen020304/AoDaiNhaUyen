namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record UserOrderDto(
    long Id,
    string OrderCode,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string District,
    string? Ward,
    string AddressLine,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ShippingFee,
    decimal TotalAmount,
    string OrderStatus,
    string? Note,
    DateTime PlacedAt,
    DateTime? ConfirmedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemDto> Items);