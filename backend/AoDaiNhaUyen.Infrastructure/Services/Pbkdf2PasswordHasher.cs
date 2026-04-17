using System.Security.Cryptography;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
  private const int Iterations = 100_000;
  private const int SaltSize = 16;
  private const int KeySize = 32;
  private const string FormatMarker = "pbkdf2-sha256";

  public string HashPassword(string password)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(password);

    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

    return string.Join(
      '$',
      FormatMarker,
      Iterations,
      Convert.ToBase64String(salt),
      Convert.ToBase64String(hash));
  }

  public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
  {
    if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
    {
      return PasswordVerificationResult.Failed;
    }

    if (!hashedPassword.StartsWith($"{FormatMarker}$", StringComparison.Ordinal))
    {
      return string.Equals(hashedPassword, providedPassword, StringComparison.Ordinal)
        ? PasswordVerificationResult.SuccessRehashNeeded
        : PasswordVerificationResult.Failed;
    }

    var parts = hashedPassword.Split('$');
    if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
    {
      return PasswordVerificationResult.Failed;
    }

    try
    {
      var salt = Convert.FromBase64String(parts[2]);
      var expected = Convert.FromBase64String(parts[3]);
      var actual = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
      var matched = CryptographicOperations.FixedTimeEquals(actual, expected);

      return matched ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
    catch (FormatException)
    {
      return PasswordVerificationResult.Failed;
    }
  }
}
