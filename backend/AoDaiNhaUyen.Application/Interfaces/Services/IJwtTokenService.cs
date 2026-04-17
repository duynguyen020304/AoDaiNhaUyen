using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IJwtTokenService
{
  string GenerateAccessToken(User user, IReadOnlyList<string> roles);
  string GenerateEmailVerificationToken(long userId);
  EmailVerificationTokenValidationResult ValidateEmailVerificationToken(string token);
  string GeneratePasswordResetToken(long userId, string secretKey);
  PasswordResetTokenValidationResult ValidatePasswordResetToken(string token, string secretKey);
}
