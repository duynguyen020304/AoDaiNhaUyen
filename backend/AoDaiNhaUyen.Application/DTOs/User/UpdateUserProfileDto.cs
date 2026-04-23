namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record UpdateUserProfileDto(
    string FullName,
    string? Phone,
    string? Gender,
    DateOnly? DateOfBirth);
