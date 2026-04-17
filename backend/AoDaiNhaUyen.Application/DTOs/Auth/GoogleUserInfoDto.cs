namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed record GoogleUserInfoDto(
  string Subject,
  string Email,
  bool EmailVerified,
  string Name,
  string? Picture);
