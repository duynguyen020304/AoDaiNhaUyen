namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record UserAddressDto(
    Guid Id,
    Guid UserId,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string District,
    string? Ward,
    string AddressLine,
    bool IsDefault,
    DateTime CreatedAt);