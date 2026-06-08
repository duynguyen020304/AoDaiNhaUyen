using System.Security.Cryptography;
using System.Text;

namespace AoDaiNhaUyen.Mcp.Auth;

public static class ApiKeyHasher
{
  public static string Hash(string apiKey, string salt)
  {
    var input = Encoding.UTF8.GetBytes(apiKey + salt);
    var hash = SHA256.HashData(input);
    return Convert.ToBase64String(hash);
  }

  public static bool Verify(string apiKey, string salt, string expectedHash)
  {
    if (string.IsNullOrWhiteSpace(apiKey)
        || string.IsNullOrWhiteSpace(salt)
        || string.IsNullOrWhiteSpace(expectedHash))
      return false;

    var actual = Convert.FromBase64String(Hash(apiKey, salt));
    byte[] expected;
    try
    {
      expected = Convert.FromBase64String(expectedHash);
    }
    catch (FormatException)
    {
      return false;
    }

    return actual.Length == expected.Length
           && CryptographicOperations.FixedTimeEquals(actual, expected);
  }
}
