using System.Text.Encodings.Web;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AuthService(
  AppDbContext dbContext,
  IPasswordHasher passwordHasher,
  IJwtTokenService jwtTokenService,
  IRefreshTokenService refreshTokenService,
  IGoogleOAuthService googleOAuthService,
  IFacebookOAuthService facebookOAuthService,
  IEmailService emailService,
  IOptions<JwtSettings> jwtSettings,
  IOptions<EmailSettings> emailSettings,
  ILogger<AuthService> logger) : IAuthService
{
  private readonly JwtSettings jwtSettings = jwtSettings.Value;
  private readonly EmailSettings emailSettings = emailSettings.Value;

  public async Task<AuthResult<string>> RegisterAsync(
    string fullName,
    string email,
    string? phone,
    string password,
    string confirmPassword,
    CancellationToken cancellationToken = default)
  {
    fullName = fullName.Trim();
    var normalizedEmail = NormalizeEmail(email);
    phone = NormalizeOptionalPhone(phone);

    if (string.IsNullOrWhiteSpace(fullName))
    {
      return AuthResult<string>.Failure("invalid_full_name", "Họ tên không được để trống.");
    }

    if (string.IsNullOrWhiteSpace(normalizedEmail))
    {
      return AuthResult<string>.Failure("invalid_email", "Email không hợp lệ.");
    }

    if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
    {
      return AuthResult<string>.Failure("invalid_password", "Mật khẩu phải có ít nhất 8 ký tự.");
    }

    if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
    {
      return AuthResult<string>.Failure("password_mismatch", "Mật khẩu xác nhận không khớp.");
    }

    var existingVerifiedCredentials = await dbContext.UserAccounts
      .Include(x => x.User)
      .FirstOrDefaultAsync(
        x => x.Provider == "credentials" &&
          x.ProviderAccountId == normalizedEmail &&
          x.IsVerified,
        cancellationToken);

    if (existingVerifiedCredentials is not null)
    {
      return AuthResult<string>.Failure("email_already_registered", "Email đã được đăng ký.");
    }

    var user = await dbContext.Users
      .Include(x => x.UserAccounts)
      .Include(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail, cancellationToken);

    if (!string.IsNullOrWhiteSpace(phone))
    {
      var phoneTaken = await dbContext.Users
        .AnyAsync(
          x => x.Phone == phone &&
            (user == null || x.Id != user.Id),
          cancellationToken);

      if (phoneTaken)
      {
        return AuthResult<string>.Failure("phone_already_registered", "Số điện thoại đã được sử dụng.");
      }
    }

    if (user is null)
    {
      user = new User
      {
        FullName = fullName,
        Email = normalizedEmail,
        Phone = phone,
        Status = "active"
      };

      dbContext.Users.Add(user);
      await dbContext.SaveChangesAsync(cancellationToken);

      var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer", cancellationToken);
      user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id });
    }
    else
    {
      user.FullName = fullName;
      user.Email = normalizedEmail;
      user.Phone = phone;
      user.Status = "active";
      user.EmailVerifiedAt = null;
      user.UpdatedAt = DateTime.UtcNow;
    }

    var account = user.UserAccounts.FirstOrDefault(x => x.Provider == "credentials");
    if (account is null)
    {
      account = new UserAccount
      {
        User = user,
        Provider = "credentials",
        ProviderAccountId = normalizedEmail,
        PasswordHash = passwordHasher.HashPassword(password),
        IsVerified = false
      };

      dbContext.UserAccounts.Add(account);
    }
    else
    {
      account.ProviderAccountId = normalizedEmail;
      account.PasswordHash = passwordHasher.HashPassword(password);
      account.IsVerified = false;
      account.UpdatedAt = DateTime.UtcNow;
    }

    var verificationToken = jwtTokenService.GenerateEmailVerificationToken(user.Id);
    var verifyLink = BuildVerifyLink(verificationToken);
    var htmlBody = BuildEmailVerificationHtml(fullName, verifyLink);

    await emailService.SendEmailAsync(
      normalizedEmail,
      "Xác thực tài khoản Ao Dai Nha Uyen",
      htmlBody,
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    return AuthResult<string>.Success("Vui lòng kiểm tra email để xác thực tài khoản.");
  }

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
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    GoogleUserInfoDto googleUser;
    try
    {
      googleUser = await googleOAuthService.ExchangeCodeForUserAsync(code, cancellationToken);
    }
    catch (GoogleOAuthExchangeException ex)
    {
      return AuthResult<AuthSessionDto>.Failure("google_exchange_failed", ex.Message);
    }

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

  public async Task<AuthResult<AuthSessionDto>> LoginWithFacebookAsync(
    string code,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    FacebookUserInfoDto facebookUser;
    try
    {
      facebookUser = await facebookOAuthService.ExchangeCodeForUserAsync(code, cancellationToken);
    }
    catch (FacebookOAuthExchangeException ex)
    {
      return AuthResult<AuthSessionDto>.Failure("facebook_exchange_failed", ex.Message);
    }

    if (string.IsNullOrWhiteSpace(facebookUser.Email))
    {
      return AuthResult<AuthSessionDto>.Failure("facebook_email_missing", "Tài khoản Facebook chưa cung cấp email.");
    }

    if (!facebookUser.EmailVerified)
    {
      return AuthResult<AuthSessionDto>.Failure("facebook_email_unverified", "Tài khoản Facebook chưa xác minh email.");
    }

    var account = await dbContext.UserAccounts
      .Include(x => x.User)
      .ThenInclude(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(
        x => x.Provider == "facebook" && x.ProviderAccountId == facebookUser.Subject,
        cancellationToken);

    if (account is null)
    {
      var normalizedEmail = NormalizeEmail(facebookUser.Email);
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
          FullName = facebookUser.Name,
          Email = normalizedEmail,
          AvatarUrl = facebookUser.Picture,
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
        existingUser.FullName = facebookUser.Name;
        existingUser.AvatarUrl = facebookUser.Picture;
        existingUser.EmailVerifiedAt ??= DateTime.UtcNow;
        existingUser.UpdatedAt = DateTime.UtcNow;
      }

      account = new UserAccount
      {
        User = existingUser,
        Provider = "facebook",
        ProviderAccountId = facebookUser.Subject,
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

  public async Task<AuthResult<AuthSessionDto>> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
  {
    var validationResult = jwtTokenService.ValidateEmailVerificationToken(token);
    if (validationResult.Status == TokenValidationStatus.Expired)
    {
      return AuthResult<AuthSessionDto>.Failure("verification_token_expired", "Lien ket xac thuc da het han.");
    }

    if (validationResult.Status != TokenValidationStatus.Valid || validationResult.UserId is null)
    {
      return AuthResult<AuthSessionDto>.Failure("verification_token_invalid", "Lien ket xac thuc khong hop le.");
    }

    var user = await dbContext.Users
      .Include(x => x.UserRoles)
      .ThenInclude(x => x.Role)
      .Include(x => x.UserAccounts)
      .FirstOrDefaultAsync(x => x.Id == validationResult.UserId.Value, cancellationToken);

    if (user is null || !IsUserActive(user))
    {
      return AuthResult<AuthSessionDto>.Failure("user_not_found", "Tai khoan khong ton tai hoac khong kha dung.");
    }

    var account = user.UserAccounts.FirstOrDefault(x => x.Provider == "credentials");
    if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash))
    {
      return AuthResult<AuthSessionDto>.Failure("verification_account_missing", "Khong tim thay tai khoan can xac thuc.");
    }

    account.IsVerified = true;
    account.UpdatedAt = DateTime.UtcNow;
    user.EmailVerifiedAt ??= DateTime.UtcNow;
    user.UpdatedAt = DateTime.UtcNow;

    var session = await CreateSessionAsync(user, null, "email-verification", cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return AuthResult<AuthSessionDto>.Success(session);
  }

  public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
  {
    var normalizedEmail = NormalizeEmail(email);
    if (string.IsNullOrWhiteSpace(normalizedEmail))
    {
      return;
    }

    var account = await dbContext.UserAccounts
      .Include(x => x.User)
      .FirstOrDefaultAsync(
        x => x.Provider == "credentials" &&
          x.ProviderAccountId == normalizedEmail &&
          x.IsVerified &&
          x.PasswordHash != null,
        cancellationToken);

    if (account is null || !IsUserActive(account.User) || string.IsNullOrWhiteSpace(account.PasswordHash))
    {
      return;
    }

    try
    {
      var signingKey = BuildPasswordResetSigningKey(account.PasswordHash);
      var token = jwtTokenService.GeneratePasswordResetToken(account.UserId, signingKey);
      var resetLink = BuildPasswordResetLink(account.UserId, token);
      var htmlBody = BuildPasswordResetHtml(account.User.FullName, resetLink);

      await emailService.SendEmailAsync(
        normalizedEmail,
        "Đặt lại mật khẩu Ao Dai Nha Uyen",
        htmlBody,
        cancellationToken);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to send password reset email for user {UserId}", account.UserId);
    }
  }

  public async Task<AuthResult<string>> ResetPasswordAsync(
    long userId,
    string token,
    string newPassword,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
    {
      return AuthResult<string>.Failure("invalid_password", "Mat khau moi phai co it nhat 8 ky tu.");
    }

    var user = await dbContext.Users
      .Include(x => x.UserAccounts)
      .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    if (user is null || !IsUserActive(user))
    {
      return AuthResult<string>.Failure("user_not_found", "Tai khoan khong ton tai hoac khong kha dung.");
    }

    var account = user.UserAccounts.FirstOrDefault(
      x => x.Provider == "credentials" &&
        x.IsVerified &&
        !string.IsNullOrWhiteSpace(x.PasswordHash));

    if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash))
    {
      return AuthResult<string>.Failure("credentials_not_found", "Khong tim thay tai khoan dang nhap bang email.");
    }

    var validationResult = jwtTokenService.ValidatePasswordResetToken(
      token,
      BuildPasswordResetSigningKey(account.PasswordHash));

    if (validationResult.Status == TokenValidationStatus.Expired)
    {
      return AuthResult<string>.Failure("reset_token_expired", "Lien ket dat lai mat khau da het han.");
    }

    if (validationResult.Status != TokenValidationStatus.Valid || validationResult.UserId != userId)
    {
      return AuthResult<string>.Failure("reset_token_invalid", "Lien ket dat lai mat khau khong hop le.");
    }

    account.PasswordHash = passwordHasher.HashPassword(newPassword);
    account.UpdatedAt = DateTime.UtcNow;
    user.UpdatedAt = DateTime.UtcNow;

    var activeSessions = await dbContext.UserSessions
      .Where(x => x.UserId == userId && !x.RevokedAt.HasValue && x.ExpiresAt > DateTime.UtcNow)
      .ToListAsync(cancellationToken);

    foreach (var session in activeSessions)
    {
      session.RevokedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return AuthResult<string>.Success("Dat lai mat khau thanh cong.");
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

  private string BuildVerifyLink(string token)
  {
    return $"{emailSettings.ApiBaseUrl.TrimEnd('/')}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
  }

  private string BuildPasswordResetLink(long userId, string token)
  {
    return $"{emailSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?id={userId}&token={Uri.EscapeDataString(token)}";
  }

  private string BuildPasswordResetSigningKey(string passwordHash)
  {
    return $"{jwtSettings.SecretKey}{passwordHash}";
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

  private static string? NormalizeOptionalPhone(string? phone)
  {
    var normalized = phone?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
  }

  private static bool IsUserActive(User user) => string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase);

  private static string BuildEmailVerificationHtml(string fullName, string verifyLink)
  {
    var safeName = HtmlEncoder.Default.Encode(fullName);
    var safeLink = HtmlEncoder.Default.Encode(verifyLink);
    return $$"""
      <!DOCTYPE html>
      <html lang="vi">
      <body style="margin:0;padding:0;background:#f6efe8;font-family:Arial,sans-serif;color:#231815;">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f6efe8;padding:24px 12px;">
          <tr>
            <td align="center">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#fffaf5;border-radius:18px;overflow:hidden;">
                <tr>
                  <td style="background:#5f0f12;padding:20px 28px;color:#f8e4cf;font-size:22px;font-weight:700;letter-spacing:0.04em;">
                    AO DAI NHA UYEN
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px 28px;">
                    <p style="margin:0 0 16px;font-size:16px;line-height:1.7;">Xin chào {{safeName}},</p>
                    <p style="margin:0 0 16px;font-size:16px;line-height:1.7;">
                      Cảm ơn bạn đã tạo tài khoản. Vui lòng xác thực email để kích hoạt tài khoản và đăng nhập ngay.
                    </p>
                    <table role="presentation" cellspacing="0" cellpadding="0" style="margin:24px 0;">
                      <tr>
                        <td bgcolor="#8b1e24" style="border-radius:999px;">
                          <a href="{{safeLink}}" style="display:inline-block;padding:14px 28px;color:#fffaf4;text-decoration:none;font-size:15px;font-weight:700;">
                            Xác thực tài khoản
                          </a>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#5b4a42;">
                      Liên kết có hiệu lực trong 24 giờ. Nếu nút không hoạt động, hãy sao chép liên kết bên dưới:
                    </p>
                    <p style="margin:0;font-size:13px;line-height:1.6;word-break:break-all;color:#8b1e24;">{{safeLink}}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
      </body>
      </html>
      """;
  }

  private static string BuildPasswordResetHtml(string fullName, string resetLink)
  {
    var safeName = HtmlEncoder.Default.Encode(fullName);
    var safeLink = HtmlEncoder.Default.Encode(resetLink);
    return $$"""
      <!DOCTYPE html>
      <html lang="vi">
      <body style="margin:0;padding:0;background:#f6efe8;font-family:Arial,sans-serif;color:#231815;">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f6efe8;padding:24px 12px;">
          <tr>
            <td align="center">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#fffaf5;border-radius:18px;overflow:hidden;">
                <tr>
                  <td style="background:#5f0f12;padding:20px 28px;color:#f8e4cf;font-size:22px;font-weight:700;letter-spacing:0.04em;">
                    AO DAI NHA UYEN
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px 28px;">
                    <p style="margin:0 0 16px;font-size:16px;line-height:1.7;">Xin chào {{safeName}},</p>
                    <p style="margin:0 0 16px;font-size:16px;line-height:1.7;">
                      Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                    </p>
                    <table role="presentation" cellspacing="0" cellpadding="0" style="margin:24px 0;">
                      <tr>
                        <td bgcolor="#8b1e24" style="border-radius:999px;">
                          <a href="{{safeLink}}" style="display:inline-block;padding:14px 28px;color:#fffaf4;text-decoration:none;font-size:15px;font-weight:700;">
                            Đặt lại mật khẩu
                          </a>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#5b4a42;">
                      Liên kết có hiệu lực trong 15 phút. Nếu bạn không yêu cầu thay đổi mật khẩu, hãy bỏ qua email này.
                    </p>
                    <p style="margin:0;font-size:13px;line-height:1.6;word-break:break-all;color:#8b1e24;">{{safeLink}}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
      </body>
      </html>
      """;
  }
}
