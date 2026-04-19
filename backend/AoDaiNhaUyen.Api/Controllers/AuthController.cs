using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
  IAuthService authService,
  IOptions<CookieSettings> cookieSettings,
  IOptions<EmailSettings> emailSettings,
  IOptions<ZaloOAuthSettings> zaloOAuthSettings,
  IWebHostEnvironment environment) : ControllerBase
{
  private const string ZaloPkceStateCookieName = "aodai_zalo_oauth_state";
  private const string ZaloPkceVerifierCookieName = "aodai_zalo_oauth_verifier";
  private readonly CookieSettings cookieSettings = cookieSettings.Value;
  private readonly EmailSettings emailSettings = emailSettings.Value;
  private readonly ZaloOAuthSettings zaloOAuthSettings = zaloOAuthSettings.Value;

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.RegisterAsync(
      request.FullName,
      request.Email,
      request.Phone,
      request.Password,
      request.ConfirmPassword,
      cancellationToken);

    if (!result.Succeeded)
    {
      return Conflict(ApiResponseFactory.Failure(
        "Dang ky that bai",
        result.ErrorCode ?? "register_failed",
        result.ErrorMessage ?? "Khong the tao tai khoan."));
    }

    return Ok(ApiResponseFactory.Success(new { registered = true }, result.Value ?? "Vui long kiem tra email de xac thuc tai khoan."));
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.LoginAsync(
      request.Email,
      request.Password,
      GetIpAddress(),
      Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.Succeeded || result.Value is null)
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Đăng nhập thất bại", result.ErrorCode ?? "login_failed", result.ErrorMessage ?? "Không thể đăng nhập."));
    }

    WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
    return Ok(ApiResponseFactory.Success(result.Value.User, "Đăng nhập thành công"));
  }

  [HttpPost("google")]
  public async Task<IActionResult> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.LoginWithGoogleAsync(
      request.Code,
      GetIpAddress(),
      Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.Succeeded || result.Value is null)
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Đăng nhập Google thất bại", result.ErrorCode ?? "google_login_failed", result.ErrorMessage ?? "Không thể đăng nhập với Google."));
    }

    WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
    return Ok(ApiResponseFactory.Success(result.Value.User, "Đăng nhập Google thành công"));
  }

  [HttpPost("zalo")]
  public async Task<IActionResult> Zalo([FromBody] ZaloLoginRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.LoginWithZaloAsync(
      request.Code,
      request.CodeVerifier,
      GetIpAddress(),
      Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.Succeeded || result.Value is null)
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Đăng nhập Zalo thất bại", result.ErrorCode ?? "zalo_login_failed", result.ErrorMessage ?? "Không thể đăng nhập với Zalo."));
    }

    WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
    return Ok(ApiResponseFactory.Success(result.Value.User, "Đăng nhập Zalo thành công"));
  }

  [HttpGet("zalo/authorize")]
  public IActionResult ZaloAuthorize()
  {
    var state = GenerateBase64Url(24);
    var codeVerifier = GenerateBase64Url(64);
    var codeChallenge = BuildPkceCodeChallenge(codeVerifier);

    Response.Cookies.Append(ZaloPkceStateCookieName, state, BuildZaloPkceCookieOptions());
    Response.Cookies.Append(ZaloPkceVerifierCookieName, codeVerifier, BuildZaloPkceCookieOptions());

    return Redirect(BuildZaloAuthorizeUrl(state, codeChallenge));
  }

  [HttpGet("zalo/callback")]
  public async Task<IActionResult> ZaloCallback(
    [FromQuery] string? code,
    [FromQuery] string? state,
    [FromQuery] string? error,
    CancellationToken cancellationToken)
  {
    try
    {
      if (!string.IsNullOrWhiteSpace(error))
      {
        return Redirect(BuildZaloFrontendRedirect("error", error));
      }

      if (string.IsNullOrWhiteSpace(code))
      {
        return Redirect(BuildZaloFrontendRedirect("error", "missing_code"));
      }

      if (!Request.Cookies.TryGetValue(ZaloPkceStateCookieName, out var expectedState) ||
          !Request.Cookies.TryGetValue(ZaloPkceVerifierCookieName, out var codeVerifier) ||
          string.IsNullOrWhiteSpace(state) ||
          !string.Equals(expectedState, state, StringComparison.Ordinal))
      {
        return Redirect(BuildZaloFrontendRedirect("error", "invalid_state"));
      }

      var result = await authService.LoginWithZaloAsync(
        code,
        codeVerifier,
        GetIpAddress(),
        Request.Headers.UserAgent.ToString(),
        cancellationToken);

      if (!result.Succeeded || result.Value is null)
      {
        ClearAuthCookies();
        return Redirect(BuildZaloFrontendRedirect("error", result.ErrorCode ?? "zalo_login_failed"));
      }

      WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
      return Redirect(BuildZaloFrontendRedirect("success", "zalo_login_success"));
    }
    finally
    {
      ClearZaloPkceCookies();
    }
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
  {
    if (!Request.Cookies.TryGetValue(cookieSettings.RefreshTokenCookieName, out var refreshToken))
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Phiên đăng nhập đã hết hạn", "missing_refresh_token", "Không tìm thấy refresh token."));
    }

    var result = await authService.RefreshAsync(
      refreshToken,
      GetIpAddress(),
      Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.Succeeded || result.Value is null)
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Phiên đăng nhập đã hết hạn", result.ErrorCode ?? "refresh_failed", result.ErrorMessage ?? "Không thể làm mới phiên đăng nhập."));
    }

    WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
    return Ok(ApiResponseFactory.Success(result.Value.User, "Làm mới phiên đăng nhập thành công"));
  }

  [HttpPost("logout")]
  public async Task<IActionResult> Logout(CancellationToken cancellationToken)
  {
    Request.Cookies.TryGetValue(cookieSettings.RefreshTokenCookieName, out var refreshToken);
    await authService.LogoutAsync(refreshToken, cancellationToken);
    ClearAuthCookies();

    return Ok(ApiResponseFactory.Success(new { loggedOut = true }, "Đăng xuất thành công"));
  }

  [HttpGet("verify-email")]
  public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
  {
    var result = await authService.VerifyEmailAsync(token, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
      var redirectUrl = BuildFrontendRedirect(
        "verified=false",
        $"reason={Uri.EscapeDataString(result.ErrorCode ?? "verification_failed")}");
      return Redirect(redirectUrl);
    }

    WriteAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
    return Redirect(BuildFrontendRedirect("verified=true", "autologin=true"));
  }

  [HttpPost("forgot-password")]
  public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
  {
    await authService.ForgotPasswordAsync(request.Email, cancellationToken);
    return Ok(ApiResponseFactory.Success(new { sent = true }, "Neu email ton tai, huong dan dat lai mat khau da duoc gui."));
  }

  [HttpPost("reset-password")]
  public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.ResetPasswordAsync(
      request.UserId,
      request.Token,
      request.NewPassword,
      cancellationToken);

    if (!result.Succeeded)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Dat lai mat khau that bai",
        result.ErrorCode ?? "reset_password_failed",
        result.ErrorMessage ?? "Khong the dat lai mat khau."));
    }

    return Ok(ApiResponseFactory.Success(new { reset = true }, result.Value ?? "Dat lai mat khau thanh cong."));
  }

  [Authorize]
  [HttpGet("me")]
  public async Task<IActionResult> Me(CancellationToken cancellationToken)
  {
    var userId = GetCurrentUserId();
    if (userId is null)
    {
      return Unauthorized(ApiResponseFactory.Failure("Không thể xác thực", "unauthorized", "Thiếu thông tin người dùng."));
    }

    var user = await authService.GetCurrentUserAsync(userId.Value, cancellationToken);
    if (user is null)
    {
      ClearAuthCookies();
      return Unauthorized(ApiResponseFactory.Failure("Không thể xác thực", "unauthorized", "Phiên đăng nhập không hợp lệ."));
    }

    return Ok(ApiResponseFactory.Success(user, "Lấy thông tin người dùng thành công"));
  }

  private void WriteAuthCookies(string accessToken, string refreshToken)
  {
    Response.Cookies.Append(
      cookieSettings.AccessTokenCookieName,
      accessToken,
      BuildCookieOptions("/", DateTimeOffset.UtcNow.AddMinutes(60)));

    Response.Cookies.Append(
      cookieSettings.RefreshTokenCookieName,
      refreshToken,
      BuildCookieOptions("/api/auth", DateTimeOffset.UtcNow.AddDays(30)));
  }

  private void ClearAuthCookies()
  {
    Response.Cookies.Delete(cookieSettings.AccessTokenCookieName, new CookieOptions { Path = "/" });
    Response.Cookies.Delete(cookieSettings.RefreshTokenCookieName, new CookieOptions { Path = "/api/auth" });
  }

  private void ClearZaloPkceCookies()
  {
    Response.Cookies.Delete(ZaloPkceStateCookieName, new CookieOptions { Path = "/" });
    Response.Cookies.Delete(ZaloPkceVerifierCookieName, new CookieOptions { Path = "/" });
  }

  private CookieOptions BuildCookieOptions(string path, DateTimeOffset expiresAt)
  {
    return new CookieOptions
    {
      HttpOnly = true,
      Secure = !environment.IsDevelopment(),
      SameSite = SameSiteMode.Lax,
      Path = path,
      Expires = expiresAt
    };
  }

  private CookieOptions BuildZaloPkceCookieOptions()
  {
    return new CookieOptions
    {
      HttpOnly = true,
      Secure = !environment.IsDevelopment(),
      SameSite = SameSiteMode.Lax,
      Path = "/",
      Expires = DateTimeOffset.UtcNow.AddMinutes(10)
    };
  }

  private string? GetIpAddress()
  {
    return HttpContext.Connection.RemoteIpAddress?.ToString();
  }

  private long? GetCurrentUserId()
  {
    var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return long.TryParse(claimValue, out var userId) ? userId : null;
  }

  private string BuildFrontendRedirect(params string[] queryParts)
  {
    var baseUrl = emailSettings.FrontendBaseUrl.TrimEnd('/');
    if (queryParts.Length == 0)
    {
      return $"{baseUrl}/login";
    }

    return $"{baseUrl}/login?{string.Join("&", queryParts)}";
  }

  private string BuildZaloFrontendRedirect(string status, string reason)
  {
    var baseUrl = emailSettings.FrontendBaseUrl.TrimEnd('/');
    return $"{baseUrl}/auth/callback/zalo?status={Uri.EscapeDataString(status)}&reason={Uri.EscapeDataString(reason)}";
  }

  private string BuildZaloAuthorizeUrl(string state, string codeChallenge)
  {
    var uriBuilder = new UriBuilder("https://oauth.zaloapp.com/v4/permission");
    var query = new Dictionary<string, string>
    {
      ["app_id"] = zaloOAuthSettings.AppId,
      ["redirect_uri"] = zaloOAuthSettings.RedirectUri,
      ["code_challenge"] = codeChallenge,
      ["state"] = state
    };

    uriBuilder.Query = string.Join(
      "&",
      query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

    return uriBuilder.Uri.ToString();
  }

  private static string GenerateBase64Url(int byteLength)
  {
    return Base64UrlEncode(RandomNumberGenerator.GetBytes(byteLength));
  }

  private static string BuildPkceCodeChallenge(string codeVerifier)
  {
    return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
  }

  private static string Base64UrlEncode(byte[] bytes)
  {
    return Convert.ToBase64String(bytes)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');
  }

  public sealed record RegisterRequest(
    string FullName,
    string Email,
    string? Phone,
    string Password,
    string ConfirmPassword);
  public sealed record LoginRequest(string Email, string Password);
  public sealed record ForgotPasswordRequest(string Email);
  public sealed record ResetPasswordRequest(long UserId, string Token, string NewPassword);
  public sealed record GoogleLoginRequest(string Code);
  public sealed record ZaloLoginRequest(string Code, string CodeVerifier);
}
