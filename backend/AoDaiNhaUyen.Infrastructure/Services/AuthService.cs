using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AuthService(
  AppDbContext dbContext,
  IPasswordHasher passwordHasher,
  IJwtTokenService jwtTokenService,
  IRefreshTokenService refreshTokenService,
  IGoogleOAuthService googleOAuthService,
  IOptions<JwtSettings> jwtSettings) : IAuthService
{
  private readonly JwtSettings jwtSettings = jwtSettings.Value;

  public async Task<AuthResult<AuthSessionDto>> LoginAsync(
    string email,
    string password,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    var normalizedEmail = NormalizeEmail(email);
    if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
    {
      return AuthResult<AuthSessionDto>.Failure("invalid_credentials", "Email hoặc mật khẩu không hợp lệ.");
    }

    var account = await dbContext.UserAccounts
      .Include(x => x.User)
      .ThenInclude(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(
        x => x.Provider == "credentials" && x.ProviderAccountId == normalizedEmail,
        cancellationToken);

    if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash) || !account.IsVerified)
    {
      return AuthResult<AuthSessionDto>.Failure("invalid_credentials", "Email hoặc mật khẩu không đúng.");
    }

    if (!IsUserActive(account.User))
    {
      return AuthResult<AuthSessionDto>.Failure("inactive_user", "Tài khoản hiện không khả dụng.");
    }

    var verification = passwordHasher.VerifyHashedPassword(account.PasswordHash, password);
    if (verification == PasswordVerificationResult.Failed)
    {
      return AuthResult<AuthSessionDto>.Failure("invalid_credentials", "Email hoặc mật khẩu không đúng.");
    }

    if (verification == PasswordVerificationResult.SuccessRehashNeeded)
    {
      account.PasswordHash = passwordHasher.HashPassword(password);
      account.UpdatedAt = DateTime.UtcNow;
    }

    var session = await CreateSessionAsync(account.User, ipAddress, userAgent, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return AuthResult<AuthSessionDto>.Success(session);
  }

  public async Task<AuthResult<AuthSessionDto>> LoginWithGoogleAsync(
    string code,
    string redirectUri,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    var googleUser = await googleOAuthService.ExchangeCodeForUserAsync(code, redirectUri, cancellationToken);
    if (!googleUser.EmailVerified)
    {
      return AuthResult<AuthSessionDto>.Failure("google_email_unverified", "Tài khoản Google chưa xác minh email.");
    }

    var account = await dbContext.UserAccounts
      .Include(x => x.User)
      .ThenInclude(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(
        x => x.Provider == "google" && x.ProviderAccountId == googleUser.Subject,
        cancellationToken);

    if (account is null)
    {
      var normalizedEmail = NormalizeEmail(googleUser.Email);
      var existingUser = await dbContext.Users
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .Include(x => x.UserAccounts)
        .FirstOrDefaultAsync(
          x => x.Email != null && x.Email.ToLower() == normalizedEmail,
          cancellationToken);

      if (existingUser is null)
      {
        existingUser = new User
        {
          FullName = googleUser.Name,
          Email = normalizedEmail,
          AvatarUrl = googleUser.Picture,
          Status = "active",
          EmailVerifiedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer", cancellationToken);
        existingUser.UserRoles.Add(new UserRole { UserId = existingUser.Id, RoleId = customerRole.Id });
      }
      else
      {
        existingUser.FullName = googleUser.Name;
        existingUser.AvatarUrl = googleUser.Picture;
        existingUser.EmailVerifiedAt ??= DateTime.UtcNow;
        existingUser.UpdatedAt = DateTime.UtcNow;
      }

      account = new UserAccount
      {
        User = existingUser,
        Provider = "google",
        ProviderAccountId = googleUser.Subject,
        IsVerified = true
      };

      dbContext.UserAccounts.Add(account);
    }

    if (!IsUserActive(account.User))
    {
      return AuthResult<AuthSessionDto>.Failure("inactive_user", "Tài khoản hiện không khả dụng.");
    }

    account.IsVerified = true;
    account.UpdatedAt = DateTime.UtcNow;

    var session = await CreateSessionAsync(account.User, ipAddress, userAgent, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return AuthResult<AuthSessionDto>.Success(session);
  }

  public async Task<AuthResult<AuthSessionDto>> RefreshAsync(
    string refreshToken,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      return AuthResult<AuthSessionDto>.Failure("missing_refresh_token", "Thiếu refresh token.");
    }

    var refreshTokenHash = refreshTokenService.HashToken(refreshToken);
    var session = await dbContext.UserSessions
      .Include(x => x.User)
      .ThenInclude(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(x => x.RefreshTokenHash == refreshTokenHash, cancellationToken);

    if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= DateTime.UtcNow || !IsUserActive(session.User))
    {
      return AuthResult<AuthSessionDto>.Failure("invalid_refresh_token", "Phiên đăng nhập đã hết hạn.");
    }

    session.RevokedAt = DateTime.UtcNow;

    var refreshedSession = await CreateSessionAsync(session.User, ipAddress, userAgent, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return AuthResult<AuthSessionDto>.Success(refreshedSession);
  }

  public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      return;
    }

    var refreshTokenHash = refreshTokenService.HashToken(refreshToken);
    var session = await dbContext.UserSessions
      .FirstOrDefaultAsync(x => x.RefreshTokenHash == refreshTokenHash, cancellationToken);

    if (session is null)
    {
      return;
    }

    var activeSessions = await dbContext.UserSessions
      .Where(x => x.UserId == session.UserId && !x.RevokedAt.HasValue && x.ExpiresAt > DateTime.UtcNow)
      .ToListAsync(cancellationToken);

    foreach (var item in activeSessions)
    {
      item.RevokedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<AuthUserDto?> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
  {
    var user = await dbContext.Users
      .Include(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    return user is null || !IsUserActive(user) ? null : MapUser(user);
  }

  private async Task<AuthSessionDto> CreateSessionAsync(
    User user,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken)
  {
    var accessToken = jwtTokenService.GenerateAccessToken(user, GetRoles(user));
    var refreshToken = refreshTokenService.GenerateToken();

    dbContext.UserSessions.Add(new UserSession
    {
      UserId = user.Id,
      RefreshTokenHash = refreshTokenService.HashToken(refreshToken),
      IpAddress = ipAddress,
      UserAgent = userAgent,
      ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays)
    });

    user.LastLoginAt = DateTime.UtcNow;
    user.UpdatedAt = DateTime.UtcNow;

    if (user.UserRoles.Count == 0)
    {
      user.UserRoles.Clear();
      user.UserRoles = await dbContext.UserRoles
        .Include(x => x.Role)
        .Where(x => x.UserId == user.Id)
        .ToListAsync(cancellationToken);
    }

    return new AuthSessionDto(MapUser(user), accessToken, refreshToken);
  }

  private static AuthUserDto MapUser(User user)
  {
    return new AuthUserDto(
      user.Id,
      user.FullName,
      user.Email,
      user.AvatarUrl,
      GetRoles(user));
  }

  private static IReadOnlyList<string> GetRoles(User user)
  {
    return user.UserRoles
      .Select(x => x.Role.Name)
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

  private static bool IsUserActive(User user) => string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase);
}
