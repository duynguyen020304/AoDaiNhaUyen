using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
  IWebHostEnvironment environment) : ControllerBase
{
  private readonly CookieSettings cookieSettings = cookieSettings.Value;

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
      request.RedirectUri,
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

  private string? GetIpAddress()
  {
    return HttpContext.Connection.RemoteIpAddress?.ToString();
  }

  private long? GetCurrentUserId()
  {
    var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return long.TryParse(claimValue, out var userId) ? userId : null;
  }

  public sealed record LoginRequest(string Email, string Password);
  public sealed record GoogleLoginRequest(string Code, string RedirectUri);
}
