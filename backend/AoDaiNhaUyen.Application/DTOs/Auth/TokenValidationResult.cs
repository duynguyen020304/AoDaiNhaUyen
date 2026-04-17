namespace AoDaiNhaUyen.Application.DTOs.Auth;

public enum TokenValidationStatus
{
  Valid = 0,
  Expired = 1,
  Invalid = 2
}

public sealed record EmailVerificationTokenValidationResult(
  TokenValidationStatus Status,
  long? UserId);

public sealed record PasswordResetTokenValidationResult(
  TokenValidationStatus Status,
  long? UserId);
