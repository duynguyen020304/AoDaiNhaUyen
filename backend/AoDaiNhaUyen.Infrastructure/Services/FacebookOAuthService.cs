using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class FacebookOAuthService(
  IHttpClientFactory httpClientFactory,
  IOptions<FacebookOAuthSettings> facebookOAuthSettings,
  ILogger<FacebookOAuthService> logger) : IFacebookOAuthService
{
  private const string GraphApiVersion = "v22.0";
  private readonly FacebookOAuthSettings facebookOAuthSettings = facebookOAuthSettings.Value;

  public async Task<FacebookUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    CancellationToken cancellationToken = default)
  {
    using var client = httpClientFactory.CreateClient();

    var tokenUri = BuildAccessTokenUri(code);
    using var tokenResponse = await client.GetAsync(tokenUri, cancellationToken);
    if (!tokenResponse.IsSuccessStatusCode)
    {
      var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning(
        "Facebook token exchange failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)tokenResponse.StatusCode,
        errorBody);
      throw new FacebookOAuthExchangeException("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.");
    }

    var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<FacebookTokenResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Facebook token response was empty.");

    var userInfoUri = BuildUserInfoUri(tokenPayload.AccessToken);
    using var userInfoResponse = await client.GetAsync(userInfoUri, cancellationToken);
    if (!userInfoResponse.IsSuccessStatusCode)
    {
      var errorBody = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning(
        "Facebook userinfo request failed with status code {StatusCode}. Response: {ResponseBody}",
        (int)userInfoResponse.StatusCode,
        errorBody);
      throw new FacebookOAuthExchangeException("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.");
    }

    var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<FacebookUserInfoResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Facebook userinfo response was empty.");

    return new FacebookUserInfoDto(
      userInfo.Id,
      userInfo.Email,
      !string.IsNullOrWhiteSpace(userInfo.Email),
      userInfo.Name,
      userInfo.Picture?.Data?.Url);
  }

  private Uri BuildAccessTokenUri(string code)
  {
    var uriBuilder = new UriBuilder($"https://graph.facebook.com/{GraphApiVersion}/oauth/access_token");
    var query = new Dictionary<string, string?>
    {
      ["client_id"] = facebookOAuthSettings.AppId,
      ["client_secret"] = facebookOAuthSettings.AppSecret,
      ["redirect_uri"] = facebookOAuthSettings.RedirectUri,
      ["code"] = code
    };

    uriBuilder.Query = string.Join(
      "&",
      query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));

    return uriBuilder.Uri;
  }

  private static Uri BuildUserInfoUri(string accessToken)
  {
    var uriBuilder = new UriBuilder($"https://graph.facebook.com/{GraphApiVersion}/me");
    var query = new Dictionary<string, string?>
    {
      ["fields"] = "id,name,email,picture",
      ["access_token"] = accessToken
    };

    uriBuilder.Query = string.Join(
      "&",
      query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));

    return uriBuilder.Uri;
  }

  private sealed record FacebookTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

  private sealed record FacebookUserInfoResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("picture")] FacebookPictureResponse? Picture);

  private sealed record FacebookPictureResponse(
    [property: JsonPropertyName("data")] FacebookPictureDataResponse? Data);

  private sealed record FacebookPictureDataResponse(
    [property: JsonPropertyName("url")] string? Url);
}
