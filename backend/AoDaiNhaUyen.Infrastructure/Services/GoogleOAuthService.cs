using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class GoogleOAuthService(
  IHttpClientFactory httpClientFactory,
  IOptions<GoogleOAuthSettings> googleOAuthSettings) : IGoogleOAuthService
{
  private readonly GoogleOAuthSettings googleOAuthSettings = googleOAuthSettings.Value;

  public async Task<GoogleUserInfoDto> ExchangeCodeForUserAsync(
    string code,
    string redirectUri,
    CancellationToken cancellationToken = default)
  {
    using var client = httpClientFactory.CreateClient();

    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["client_id"] = googleOAuthSettings.ClientId,
        ["client_secret"] = googleOAuthSettings.ClientSecret,
        ["code"] = code,
        ["grant_type"] = "authorization_code",
        ["redirect_uri"] = redirectUri
      })
    };

    using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
    tokenResponse.EnsureSuccessStatusCode();

    var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Google token response was empty.");

    using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
    userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

    using var userInfoResponse = await client.SendAsync(userInfoRequest, cancellationToken);
    userInfoResponse.EnsureSuccessStatusCode();

    var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken: cancellationToken)
      ?? throw new InvalidOperationException("Google userinfo response was empty.");

    return new GoogleUserInfoDto(
      userInfo.Sub,
      userInfo.Email,
      userInfo.EmailVerified,
      userInfo.Name,
      userInfo.Picture);
  }

  private sealed record GoogleTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

  private sealed record GoogleUserInfoResponse(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("email_verified")] bool EmailVerified,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("picture")] string? Picture);
}
