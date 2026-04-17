using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAuthService
{
  Task<AuthResult<string>> RegisterAsync(
    string fullName,
    string email,
    string? phone,
    string password,
    string confirmPassword,
    CancellationToken cancellationToken = default);

  Task<AuthResult<AuthSessionDto>> LoginAsync(
    string email,
    string password,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

  Task<AuthResult<AuthSessionDto>> LoginWithGoogleAsync(
    string code,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

  Task<AuthResult<AuthSessionDto>> LoginWithFacebookAsync(
    string code,
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

  Task<AuthResult<AuthSessionDto>> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

  Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

  Task<AuthResult<string>> ResetPasswordAsync(
    long userId,
    string token,
    string newPassword,
    CancellationToken cancellationToken = default);
}
