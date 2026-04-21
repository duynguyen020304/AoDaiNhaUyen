using System.Net.Http.Json;
using System.Text.Json;
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
        ["code_verifier"] = codeVerifier,
        ["redirect_uri"] = zaloOAuthSettings.RedirectUri
      })
    };
    tokenRequest.Headers.TryAddWithoutValidation("secret_key", zaloOAuthSettings.SecretKey);

    using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
    var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
    var tokenPayload = DeserializeResponse<ZaloTokenResponse>(tokenResponseBody, "token");

    if (!tokenResponse.IsSuccessStatusCode)
    {
      logger.LogWarning(
        "Zalo token exchange failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)tokenResponse.StatusCode,
        tokenResponseBody);
      throw new ZaloOAuthExchangeException(GetZaloErrorMessage(
        tokenPayload.ErrorName,
        tokenPayload.Error,
        tokenPayload.Message,
        "Không thể xác minh đăng nhập Zalo. Vui lòng thử lại."));
    }

    if (HasZaloError(tokenPayload.ErrorName, tokenPayload.Error))
    {
      logger.LogWarning(
        "Zalo token exchange returned success status but included error payload. Response: {ResponseBody}",
        tokenResponseBody);
      throw new ZaloOAuthExchangeException(GetZaloErrorMessage(
        tokenPayload.ErrorName,
        tokenPayload.Error,
        tokenPayload.Message,
        "Không thể xác minh đăng nhập Zalo. Vui lòng thử lại."));
    }

    if (string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
    {
      logger.LogWarning(
        "Zalo token exchange returned success status but missing access_token. Response: {ResponseBody}",
        tokenResponseBody);
      throw new ZaloOAuthExchangeException("Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.");
    }

    var userInfoUri = BuildUserInfoUri();
    using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUri);
    userInfoRequest.Headers.TryAddWithoutValidation("access_token", tokenPayload.AccessToken);

    using var userInfoResponse = await client.SendAsync(userInfoRequest, cancellationToken);
    var userInfoResponseBody = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
    var userInfo = DeserializeResponse<ZaloUserInfoResponse>(userInfoResponseBody, "userinfo");

    if (!userInfoResponse.IsSuccessStatusCode)
    {
      logger.LogWarning(
        "Zalo userinfo request failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)userInfoResponse.StatusCode,
        userInfoResponseBody);
      throw new ZaloOAuthExchangeException(GetZaloErrorMessage(
        userInfo.ErrorName,
        userInfo.Error,
        userInfo.Message,
        "Không thể lấy thông tin tài khoản Zalo. Vui lòng thử lại."));
    }

    if (HasZaloError(userInfo.ErrorName, userInfo.Error))
    {
      logger.LogWarning(
        "Zalo userinfo returned success status but included error payload. Response: {ResponseBody}",
        userInfoResponseBody);
      throw new ZaloOAuthExchangeException(GetZaloErrorMessage(
        userInfo.ErrorName,
        userInfo.Error,
        userInfo.Message,
        "Không thể lấy thông tin tài khoản Zalo. Vui lòng thử lại."));
    }

    if (string.IsNullOrWhiteSpace(userInfo.Id))
    {
      logger.LogWarning(
        "Zalo userinfo returned success status but missing id. Response: {ResponseBody}",
        userInfoResponseBody);
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

  private static T DeserializeResponse<T>(string responseBody, string responseName)
  {
    return JsonSerializer.Deserialize<T>(responseBody)
      ?? throw new InvalidOperationException($"Zalo {responseName} response was empty.");
  }

  private static bool HasZaloError(string? errorName, int? error)
  {
    return !string.IsNullOrWhiteSpace(errorName) || error is not null and not 0;
  }

  private static string GetZaloErrorMessage(
    string? errorName,
    int? error,
    string? message,
    string fallbackMessage)
  {
    if (!string.IsNullOrWhiteSpace(message))
    {
      return message;
    }

    if (!string.IsNullOrWhiteSpace(errorName))
    {
      return $"{fallbackMessage} ({errorName})";
    }

    if (error is not null and not 0)
    {
      return $"{fallbackMessage} (mã lỗi {error})";
    }

    return fallbackMessage;
  }

  private sealed record ZaloTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("error")] int? Error,
    [property: JsonPropertyName("error_name")] string? ErrorName,
    [property: JsonPropertyName("message")] string? Message);

  private sealed record ZaloUserInfoResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("picture")] ZaloPictureResponse? Picture,
    [property: JsonPropertyName("error")] int? Error,
    [property: JsonPropertyName("error_name")] string? ErrorName,
    [property: JsonPropertyName("message")] string? Message);

  private sealed record ZaloPictureResponse(
    [property: JsonPropertyName("data")] ZaloPictureDataResponse? Data);

  private sealed record ZaloPictureDataResponse(
    [property: JsonPropertyName("url")] string? Url);
}
