namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed record FacebookUserInfoDto(
  string Subject,
  string? Email,
  bool EmailVerified,
  string Name,
  string? Picture);
