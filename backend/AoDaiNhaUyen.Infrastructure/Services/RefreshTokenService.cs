using System.Security.Cryptography;
using System.Text;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class RefreshTokenService(IOptions<JwtSettings> jwtSettings) : IRefreshTokenService
{
  private readonly JwtSettings jwtSettings = jwtSettings.Value;

  public string GenerateToken()
  {
    var tokenBytes = RandomNumberGenerator.GetBytes(48);
    return Convert.ToBase64String(tokenBytes)
      .Replace('+', '-')
      .Replace('/', '_')
      .TrimEnd('=');
  }

  public string HashToken(string token)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(token);

    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
    using var hmac = new HMACSHA256(key);
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(hash);
  }
}
