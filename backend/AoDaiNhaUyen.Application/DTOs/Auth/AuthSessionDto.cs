namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed record AuthSessionDto(
  AuthUserDto User,
  string AccessToken,
  string RefreshToken);
