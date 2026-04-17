namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed record AuthUserDto(
  long Id,
  string FullName,
  string? Email,
  string? AvatarUrl,
  IReadOnlyList<string> Roles);
