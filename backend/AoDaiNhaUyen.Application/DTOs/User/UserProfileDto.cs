namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record UserProfileDto(
    long Id,
    string FullName,
    string? Email,
    string? Phone,
    string? Gender,
    DateOnly? DateOfBirth,
    string? AvatarUrl,
    string Status,
    DateTime? EmailVerifiedAt,
    DateTime? PhoneVerifiedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);