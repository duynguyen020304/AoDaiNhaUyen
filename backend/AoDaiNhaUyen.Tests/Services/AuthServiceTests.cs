using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
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
      zaloOAuthService: new StubZaloOAuthService());

    var result = await service.LoginWithGoogleAsync("bad-code", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("google_exchange_failed", result.ErrorCode);
    Assert.Equal("Không thể xác minh đăng nhập Google. Vui lòng thử lại.", result.ErrorMessage);
  }

  [Fact]
  public async Task LoginWithZaloAsync_ReturnsFailure_WhenZaloExchangeFails()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      zaloOAuthService: new ThrowingZaloOAuthService());

    var result = await service.LoginWithZaloAsync("bad-code", "code-verifier", "127.0.0.1", "test-agent");

    Assert.False(result.Succeeded);
    Assert.Null(result.Value);
    Assert.Equal("zalo_exchange_failed", result.ErrorCode);
    Assert.Equal("Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.", result.ErrorMessage);
  }

  [Fact]
  public async Task LoginWithZaloAsync_CreatesUserAccountAndSessionWithoutEmailOrPhone()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      zaloOAuthService: new StubZaloOAuthService());

    var result = await service.LoginWithZaloAsync("auth-code", "code-verifier", "127.0.0.1", "test-agent");

    Assert.True(result.Succeeded);
    Assert.NotNull(result.Value);
    Assert.Equal("Uyen Zalo", result.Value.User.FullName);
    Assert.Null(result.Value.User.Email);

    var user = await dbContext.Users.Include(x => x.UserAccounts).Include(x => x.Sessions).SingleAsync();
    Assert.Null(user.Email);
    Assert.Null(user.Phone);
    Assert.Equal("https://example.com/avatar.png", user.AvatarUrl);
    Assert.Contains(user.UserAccounts, account => account.Provider == "zalo" && account.ProviderAccountId == "zalo-user" && account.IsVerified);
    Assert.Single(user.Sessions);
  }

  [Fact]
  public async Task LoginWithZaloAsync_ReusesExistingAccountAndUpdatesProfile()
  {
    await using var dbContext = CreateDbContext();
    var user = new User
    {
      FullName = "Old Name",
      AvatarUrl = "https://example.com/old.png",
      Status = "active"
    };
    user.UserAccounts.Add(new UserAccount
    {
      Provider = "zalo",
      ProviderAccountId = "zalo-user",
      IsVerified = true
    });
    user.UserRoles.Add(new UserRole { RoleId = 1 });
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var service = CreateService(
      dbContext,
      googleOAuthService: new StubGoogleOAuthService(),
      zaloOAuthService: new UpdatedZaloOAuthService());

    var result = await service.LoginWithZaloAsync("auth-code", "code-verifier", "127.0.0.1", "test-agent");

    Assert.True(result.Succeeded);

    var accounts = await dbContext.UserAccounts.Where(x => x.Provider == "zalo").ToListAsync();
    Assert.Single(accounts);
    var updatedUser = await dbContext.Users.Include(x => x.Sessions).SingleAsync();
    Assert.Equal("Updated Zalo", updatedUser.FullName);
    Assert.Equal("https://example.com/new.png", updatedUser.AvatarUrl);
    Assert.Single(updatedUser.Sessions);
  }

  private static AppDbContext CreateDbContext()
  {
    var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
    dbContext.Roles.Add(new Role { Id = 1, Name = "customer" });
    dbContext.SaveChanges();
    return dbContext;
  }

  private static AuthService CreateService(
    AppDbContext dbContext,
    IGoogleOAuthService googleOAuthService,
    IZaloOAuthService zaloOAuthService)
  {
    return new AuthService(
      dbContext,
      new StubPasswordHasher(),
      new StubJwtTokenService(),
      new StubRefreshTokenService(),
      googleOAuthService,
      zaloOAuthService,
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

  private sealed class ThrowingZaloOAuthService : IZaloOAuthService
  {
    public Task<ZaloUserInfoDto> ExchangeCodeForUserAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
      throw new ZaloOAuthExchangeException("Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.");
    }
  }

  private sealed class StubZaloOAuthService : IZaloOAuthService
  {
    public Task<ZaloUserInfoDto> ExchangeCodeForUserAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new ZaloUserInfoDto("zalo-user", "Uyen Zalo", "https://example.com/avatar.png"));
    }
  }

  private sealed class UpdatedZaloOAuthService : IZaloOAuthService
  {
    public Task<ZaloUserInfoDto> ExchangeCodeForUserAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new ZaloUserInfoDto("zalo-user", "Updated Zalo", "https://example.com/new.png"));
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
