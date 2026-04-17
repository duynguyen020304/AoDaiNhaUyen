using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAuthService
{
  Task<AuthResult<AuthSessionDto>> LoginAsync(
    string email,
    string password,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

  Task<AuthResult<AuthSessionDto>> LoginWithGoogleAsync(
    string code,
    string redirectUri,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

  Task<AuthResult<AuthSessionDto>> RefreshAsync(
    string refreshToken,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

  Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

  Task<AuthUserDto?> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
}
