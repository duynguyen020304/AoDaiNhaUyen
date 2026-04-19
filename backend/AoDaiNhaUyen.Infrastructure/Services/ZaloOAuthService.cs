using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ZaloOAuthService(
  IHttpClientFactory httpClientFactory,
  IOptions<ZaloOAuthSettings> zaloOAuthSettings,
  ILogger<ZaloOAuthService> logger) : IZaloOAuthService
{
  private readonly ZaloOAuthSettings zaloOAuthSettings = zaloOAuthSettings.Value;

  public async Task<ZaloUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    string codeVerifier,
    CancellationToken cancellationToken = default)
  {
    using var client = httpClientFactory.CreateClient();

    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth.zaloapp.com/v4/access_token")
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["code"] = code,
        ["app_id"] = zaloOAuthSettings.AppId,
        ["grant_type"] = "authorization_code",
        ["code_verifier"] = codeVerifier
      })
    };
    tokenRequest.Headers.TryAddWithoutValidation("secret_key", zaloOAuthSettings.SecretKey);

    using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
    if (!tokenResponse.IsSuccessStatusCode)
    {
      var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning(
        "Zalo token exchange failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)tokenResponse.StatusCode,
        errorBody);
      throw new ZaloOAuthExchangeException("Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.");
    }

    var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<ZaloTokenResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Zalo token response was empty.");

    var userInfoUri = BuildUserInfoUri();
    using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUri);
    userInfoRequest.Headers.TryAddWithoutValidation("access_token", tokenPayload.AccessToken);

    using var userInfoResponse = await client.SendAsync(userInfoRequest, cancellationToken);
    if (!userInfoResponse.IsSuccessStatusCode)
    {
      var errorBody = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning(
        "Zalo userinfo request failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)userInfoResponse.StatusCode,
        errorBody);
      throw new ZaloOAuthExchangeException("Không thể lấy thông tin tài khoản Zalo. Vui lòng thử lại.");
    }

    var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<ZaloUserInfoResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Zalo userinfo response was empty.");

    if (string.IsNullOrWhiteSpace(userInfo.Id))
    {
      throw new ZaloOAuthExchangeException("Không thể lấy mã định danh tài khoản Zalo. Vui lòng thử lại.");
    }

    return new ZaloUserInfoDto(
      userInfo.Id,
      string.IsNullOrWhiteSpace(userInfo.Name) ? "Khách hàng Zalo" : userInfo.Name,
      userInfo.Picture?.Data?.Url);
  }

  private static Uri BuildUserInfoUri()
  {
    var uriBuilder = new UriBuilder("https://graph.zalo.me/v2.0/me");
    var query = new Dictionary<string, string?>
    {
      ["fields"] = "id,name,picture"
    };

    uriBuilder.Query = string.Join(
      "&",
      query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));

    return uriBuilder.Uri;
  }

  private sealed record ZaloTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

  private sealed record ZaloUserInfoResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("picture")] ZaloPictureResponse? Picture);

  private sealed record ZaloPictureResponse(
    [property: JsonPropertyName("data")] ZaloPictureDataResponse? Data);

  private sealed record ZaloPictureDataResponse(
    [property: JsonPropertyName("url")] string? Url);
}
