using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class AuthServiceTests
{
  [Fact]
  public async Task LoginWithGoogleAsync_ReturnsFailure_WhenGoogleExchangeFails()
  {
    await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
    var service = new AuthService(
      dbContext,
      new StubPasswordHasher(),
      new StubJwtTokenService(),
      new StubRefreshTokenService(),
      new ThrowingGoogleOAuthService(),
      Options.Create(new JwtSettings()));

    var result = await service.LoginWithGoogleAsync("bad-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("google_exchange_failed", result.ErrorCode);
    Assert.Equal("Không thể xác minh đăng nhập Google. Vui lòng thử lại.", result.ErrorMessage);
  }

  private sealed class ThrowingGoogleOAuthService : IGoogleOAuthService
  {
    public Task<AoDaiNhaUyen.Application.DTOs.Auth.GoogleUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      throw new GoogleOAuthExchangeException("Không thể xác minh đăng nhập Google. Vui lòng thử lại.");
    }
  }

  private sealed class StubPasswordHasher : IPasswordHasher
  {
    public string HashPassword(string password) => password;
    public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword) => PasswordVerificationResult.Failed;
  }

  private sealed class StubJwtTokenService : IJwtTokenService
  {
    public string GenerateAccessToken(AoDaiNhaUyen.Domain.Entities.User user, IReadOnlyList<string> roles) => "token";
  }

  private sealed class StubRefreshTokenService : IRefreshTokenService
  {
    public string GenerateToken() => "refresh-token";
    public string HashToken(string token) => token;
  }
}
