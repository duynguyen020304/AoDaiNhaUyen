using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class AuthServiceTests
{
  [Fact]
  public async Task LoginWithGoogleAsync_ReturnsFailure_WhenGoogleExchangeFails()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new ThrowingGoogleOAuthService(),
      facebookOAuthService: new StubFacebookOAuthService());

    var result = await service.LoginWithGoogleAsync("bad-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("google_exchange_failed", result.ErrorCode);
    Assert.Equal("Không thể xác minh đăng nhập Google. Vui lòng thử lại.", result.ErrorMessage);
  }

  [Fact]
  public async Task LoginWithFacebookAsync_ReturnsFailure_WhenFacebookExchangeFails()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      facebookOAuthService: new ThrowingFacebookOAuthService());

    var result = await service.LoginWithFacebookAsync("bad-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("facebook_exchange_failed", result.ErrorCode);
    Assert.Equal("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.", result.ErrorMessage);
  }

  [Fact]
  public async Task LoginWithFacebookAsync_ReturnsFailure_WhenFacebookEmailIsMissing()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      facebookOAuthService: new MissingEmailFacebookOAuthService());

    var result = await service.LoginWithFacebookAsync("auth-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("facebook_email_missing", result.ErrorCode);
    Assert.Equal("Tài khoản Facebook chưa cung cấp email.", result.ErrorMessage);
  }

  [Fact]
  public async Task LoginWithFacebookAsync_ReturnsFailure_WhenFacebookEmailIsNotVerified()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      facebookOAuthService: new UnverifiedFacebookOAuthService());

    var result = await service.LoginWithFacebookAsync("auth-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("facebook_email_unverified", result.ErrorCode);
    Assert.Equal("Tài khoản Facebook chưa xác minh email.", result.ErrorMessage);
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
  }

  private static AuthService CreateService(
    AppDbContext dbContext,
    IGoogleOAuthService googleOAuthService,
    IFacebookOAuthService facebookOAuthService)
  {
    return new AuthService(
      dbContext,
      new StubPasswordHasher(),
      new StubJwtTokenService(),
      new StubRefreshTokenService(),
      googleOAuthService,
      facebookOAuthService,
      new StubEmailService(),
      Options.Create(new JwtSettings { SecretKey = "abcdefghijklmnopqrstuvwxyz123456" }),
      Options.Create(new EmailSettings
      {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpUsername = "user",
        SmtpPassword = "password",
        FromEmail = "noreply@example.com",
        FromName = "Ao Dai Nha Uyen",
        ApiBaseUrl = "http://localhost:5043",
        FrontendBaseUrl = "http://localhost:5173"
      }),
      NullLogger<AuthService>.Instance);
  }

  private sealed class ThrowingGoogleOAuthService : IGoogleOAuthService
  {
    public Task<GoogleUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      throw new GoogleOAuthExchangeException("Không thể xác minh đăng nhập Google. Vui lòng thử lại.");
    }
  }

  private sealed class StubGoogleOAuthService : IGoogleOAuthService
  {
    public Task<GoogleUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new GoogleUserInfoDto("google-user", "uyen@example.com", true, "Uyen", null));
    }
  }

  private sealed class ThrowingFacebookOAuthService : IFacebookOAuthService
  {
    public Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      throw new FacebookOAuthExchangeException("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.");
    }
  }

  private sealed class StubFacebookOAuthService : IFacebookOAuthService
  {
    public Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new FacebookUserInfoDto("facebook-user", "uyen@example.com", true, "Uyen", null));
    }
  }

  private sealed class MissingEmailFacebookOAuthService : IFacebookOAuthService
  {
    public Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new FacebookUserInfoDto("facebook-user", null, false, "Uyen", null));
    }
  }

  private sealed class UnverifiedFacebookOAuthService : IFacebookOAuthService
  {
    public Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(string code, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new FacebookUserInfoDto("facebook-user", "uyen@example.com", false, "Uyen", null));
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
    public string GenerateEmailVerificationToken(long userId) => "verification-token";
    public EmailVerificationTokenValidationResult ValidateEmailVerificationToken(string token) => new(TokenValidationStatus.Invalid, null);
    public string GeneratePasswordResetToken(long userId, string secretKey) => "password-reset-token";
    public PasswordResetTokenValidationResult ValidatePasswordResetToken(string token, string secretKey) => new(TokenValidationStatus.Invalid, null);
  }

  private sealed class StubRefreshTokenService : IRefreshTokenService
  {
    public string GenerateToken() => "refresh-token";
    public string HashToken(string token) => token;
  }

  private sealed class StubEmailService : IEmailService
  {
    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }
  }
}
