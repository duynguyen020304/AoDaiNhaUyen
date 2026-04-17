namespace AoDaiNhaUyen.Application.Options;

public sealed class CookieSettings
{
  public string AccessTokenCookieName { get; set; } = "access_token";
  public string RefreshTokenCookieName { get; set; } = "refresh_token";
}
