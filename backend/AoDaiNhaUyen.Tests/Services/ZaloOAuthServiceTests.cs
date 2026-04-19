using System.Net;
using System.Text;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ZaloOAuthServiceTests
{
  [Fact]
  public async Task ExchangeCodeForUserAsync_UsesSecretHeaderAndCodeVerifier_AndReturnsUser()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{\"access_token\":\"token-123\",\"refresh_token\":\"refresh-123\",\"expires_in\":90000}", Encoding.UTF8, "application/json")
      },
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(
          "{\"id\":\"zalo-user\",\"name\":\"Uyen\",\"picture\":{\"data\":{\"url\":\"https://example.com/avatar.png\"}}}",
          Encoding.UTF8,
          "application/json")
      }
    ]);
    var service = new ZaloOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new ZaloOAuthSettings
      {
        AppId = "app-id",
        SecretKey = "secret-key",
        RedirectUri = "http://localhost:5173/auth/callback/zalo"
      }),
      NullLogger<ZaloOAuthService>.Instance);

    var result = await service.ExchangeCodeForUserAsync("auth-code", "code-verifier");

    Assert.Equal("zalo-user", result.Subject);
    Assert.Equal("Uyen", result.Name);
    Assert.Equal("https://example.com/avatar.png", result.Picture);

    Assert.Collection(
      handler.Requests,
      request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://oauth.zaloapp.com/v4/access_token", request.RequestUri?.AbsoluteUri);
        Assert.True(request.Headers.TryGetValues("secret_key", out var values));
        Assert.Contains("secret-key", values);
        var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.Contains("grant_type=authorization_code", body);
        Assert.Contains("code=auth-code", body);
        Assert.Contains("app_id=app-id", body);
        Assert.Contains("code_verifier=code-verifier", body);
      },
      request =>
      {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://graph.zalo.me/v2.0/me?fields=id%2Cname%2Cpicture", request.RequestUri?.AbsoluteUri);
        Assert.True(request.Headers.TryGetValues("access_token", out var values));
        Assert.Contains("token-123", values);
      });
  }

  [Fact]
  public async Task ExchangeCodeForUserAsync_ThrowsZaloOAuthExchangeException_WhenZaloReturnsBadRequest()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("{\"error\":-201,\"message\":\"invalid code\"}", Encoding.UTF8, "application/json")
      }
    ]);
    var service = CreateService(handler);

    var exception = await Assert.ThrowsAsync<ZaloOAuthExchangeException>(() => service.ExchangeCodeForUserAsync("bad-code", "code-verifier"));

    Assert.Equal("Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.", exception.Message);
  }

  [Fact]
  public async Task ExchangeCodeForUserAsync_ThrowsZaloOAuthExchangeException_WhenZaloUserInfoFails()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{\"access_token\":\"token-123\"}", Encoding.UTF8, "application/json")
      },
      new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("{\"error\":-216,\"message\":\"invalid token\"}", Encoding.UTF8, "application/json")
      }
    ]);
    var service = CreateService(handler);

    var exception = await Assert.ThrowsAsync<ZaloOAuthExchangeException>(() => service.ExchangeCodeForUserAsync("auth-code", "code-verifier"));

    Assert.Equal("Không thể lấy thông tin tài khoản Zalo. Vui lòng thử lại.", exception.Message);
  }

  private static ZaloOAuthService CreateService(StubHttpMessageHandler handler)
  {
    return new ZaloOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new ZaloOAuthSettings
      {
        AppId = "app-id",
        SecretKey = "secret-key",
        RedirectUri = "http://localhost:5173/auth/callback/zalo"
      }),
      NullLogger<ZaloOAuthService>.Instance);
  }

  private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => client;
  }

  private sealed class StubHttpMessageHandler(IReadOnlyList<HttpResponseMessage> responses) : HttpMessageHandler
  {
    private int index;

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      Requests.Add(CloneRequest(request));
      return Task.FromResult(responses[index++]);
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
      var clone = new HttpRequestMessage(request.Method, request.RequestUri);
      if (request.Content is not null)
      {
        clone.Content = new StringContent(request.Content.ReadAsStringAsync().GetAwaiter().GetResult(), Encoding.UTF8, request.Content.Headers.ContentType?.MediaType);
      }

      foreach (var header in request.Headers)
      {
        clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }

      return clone;
    }
  }
}
