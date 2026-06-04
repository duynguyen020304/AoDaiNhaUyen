namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed record AuthUserDto(
  Guid Id,
  string FullName,
  string? Email,
  string? AvatarUrl,
  IReadOnlyList<string> Roles);
