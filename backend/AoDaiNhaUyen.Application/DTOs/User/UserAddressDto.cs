namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record UserAddressDto(
    long Id,
    long UserId,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string District,
    string? Ward,
    string AddressLine,
    bool IsDefault,
    DateTime CreatedAt);