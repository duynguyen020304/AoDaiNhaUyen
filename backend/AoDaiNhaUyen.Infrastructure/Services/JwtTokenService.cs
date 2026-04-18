using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtSettings> jwtSettings) : IJwtTokenService
{
  private readonly JwtSettings jwtSettings = jwtSettings.Value;
  private const string EmailVerificationTokenType = "email_verification";
  private const string PasswordResetTokenType = "password_reset";

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

  public string GenerateEmailVerificationToken(long userId)
  {
    return WriteToken(
      new List<Claim>
      {
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new("token_type", EmailVerificationTokenType),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
      },
      DateTime.UtcNow.AddHours(24),
      jwtSettings.SecretKey);
  }

  public EmailVerificationTokenValidationResult ValidateEmailVerificationToken(string token)
  {
    return ValidateToken(
      token,
      jwtSettings.SecretKey,
      EmailVerificationTokenType,
      userId => new EmailVerificationTokenValidationResult(TokenValidationStatus.Valid, userId),
      userId => new EmailVerificationTokenValidationResult(TokenValidationStatus.Expired, userId),
      () => new EmailVerificationTokenValidationResult(TokenValidationStatus.Invalid, null));
  }

  public string GeneratePasswordResetToken(long userId, string secretKey)
  {
    return WriteToken(
      new List<Claim>
      {
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new("token_type", PasswordResetTokenType),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
      },
      DateTime.UtcNow.AddMinutes(15),
      secretKey);
  }

  public PasswordResetTokenValidationResult ValidatePasswordResetToken(string token, string secretKey)
  {
    return ValidateToken(
      token,
      secretKey,
      PasswordResetTokenType,
      userId => new PasswordResetTokenValidationResult(TokenValidationStatus.Valid, userId),
      userId => new PasswordResetTokenValidationResult(TokenValidationStatus.Expired, userId),
      () => new PasswordResetTokenValidationResult(TokenValidationStatus.Invalid, null));
  }

  private string WriteToken(IEnumerable<Claim> claims, DateTime expiresAt, string secretKey)
  {
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: jwtSettings.Issuer,
      audience: jwtSettings.Audience,
      claims: claims,
      expires: expiresAt,
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private T ValidateToken<T>(
    string token,
    string secretKey,
    string expectedTokenType,
    Func<long, T> onValid,
    Func<long?, T> onExpired,
    Func<T> onInvalid)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return onInvalid();
    }

    var handler = new JwtSecurityTokenHandler();
    try
    {
      var principal = handler.ValidateToken(token, BuildValidationParameters(secretKey, validateLifetime: true), out _);
      var tokenType = principal.FindFirst("token_type")?.Value;
      var userId = GetUserId(principal);
      if (!string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal) || userId is null)
      {
        return onInvalid();
      }

      return onValid(userId.Value);
    }
    catch (SecurityTokenExpiredException)
    {
      try
      {
        var principal = handler.ValidateToken(token, BuildValidationParameters(secretKey, validateLifetime: false), out _);
        var tokenType = principal.FindFirst("token_type")?.Value;
        var userId = GetUserId(principal);
        if (!string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal))
        {
          return onInvalid();
        }

        return onExpired(userId);
      }
      catch
      {
        return onInvalid();
      }
    }
    catch
    {
      return onInvalid();
    }
  }

  private TokenValidationParameters BuildValidationParameters(string secretKey, bool validateLifetime)
  {
    return new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = validateLifetime,
      ValidateIssuerSigningKey = true,
      ValidIssuer = jwtSettings.Issuer,
      ValidAudience = jwtSettings.Audience,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
      ClockSkew = TimeSpan.FromMinutes(1)
    };
  }

  private static long? GetUserId(ClaimsPrincipal principal)
  {
    var raw = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    return long.TryParse(raw, out var userId) ? userId : null;
  }
}
