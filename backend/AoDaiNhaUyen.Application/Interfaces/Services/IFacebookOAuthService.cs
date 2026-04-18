using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IFacebookOAuthService
{
  Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    CancellationToken cancellationToken = default);
}
