using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IJwtTokenService
{
  string GenerateAccessToken(User user, IReadOnlyList<string> roles);
  string GenerateEmailVerificationToken(Guid userId);
  EmailVerificationTokenValidationResult ValidateEmailVerificationToken(string token);
  string GeneratePasswordResetToken(Guid userId, string secretKey);
  PasswordResetTokenValidationResult ValidatePasswordResetToken(string token, string secretKey);
}
