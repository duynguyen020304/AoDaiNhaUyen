using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IZaloOAuthService
{
  Task<ZaloUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    string codeVerifier,
    CancellationToken cancellationToken = default);
}
