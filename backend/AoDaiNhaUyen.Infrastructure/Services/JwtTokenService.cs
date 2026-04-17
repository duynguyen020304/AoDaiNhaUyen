using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtSettings> jwtSettings) : IJwtTokenService
{
  private readonly JwtSettings jwtSettings = jwtSettings.Value;

  public string GenerateAccessToken(User user, IReadOnlyList<string> roles)
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
      new(ClaimTypes.Name, user.FullName)
    };

    if (!string.IsNullOrWhiteSpace(user.Email))
    {
      claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
      claims.Add(new Claim(ClaimTypes.Email, user.Email));
    }

    foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes);

    var token = new JwtSecurityToken(
      issuer: jwtSettings.Issuer,
      audience: jwtSettings.Audience,
      claims: claims,
      expires: expiresAt,
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
