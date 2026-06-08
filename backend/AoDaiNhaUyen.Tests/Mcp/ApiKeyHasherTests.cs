using AoDaiNhaUyen.Mcp.Auth;

namespace AoDaiNhaUyen.Tests.Mcp;

public sealed class ApiKeyHasherTests
{
  [Fact]
  public void Verify_ReturnsTrue_ForMatchingKeyAndSalt()
  {
    const string key = "mcp_live_test_key";
    const string salt = "test-salt";
    var hash = ApiKeyHasher.Hash(key, salt);

    Assert.True(ApiKeyHasher.Verify(key, salt, hash));
  }

  [Fact]
  public void Verify_ReturnsFalse_ForWrongKey()
  {
    const string salt = "test-salt";
    var hash = ApiKeyHasher.Hash("mcp_live_test_key", salt);

    Assert.False(ApiKeyHasher.Verify("wrong", salt, hash));
  }

  [Fact]
  public void Verify_ReturnsFalse_ForInvalidStoredHash()
  {
    Assert.False(ApiKeyHasher.Verify("mcp_live_test_key", "test-salt", "not-base64"));
  }
}
