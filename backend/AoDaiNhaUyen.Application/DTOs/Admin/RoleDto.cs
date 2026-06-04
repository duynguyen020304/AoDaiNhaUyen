namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Represents a role in the system.</summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description);