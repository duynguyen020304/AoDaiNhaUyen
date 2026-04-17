using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IGoogleOAuthService
{
  Task<GoogleUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    CancellationToken cancellationToken = default);
}
