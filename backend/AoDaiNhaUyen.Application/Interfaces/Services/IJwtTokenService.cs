using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IJwtTokenService
{
  string GenerateAccessToken(User user, IReadOnlyList<string> roles);
}
