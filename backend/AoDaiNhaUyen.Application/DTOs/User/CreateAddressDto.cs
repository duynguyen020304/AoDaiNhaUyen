namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record CreateAddressDto(
    string RecipientName,
    string RecipientPhone,
    string Province,
    string District,
    string? Ward,
    string AddressLine,
    bool IsDefault);