namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Represents a user in the admin list view.</summary>
public sealed record AdminUserListItemDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);