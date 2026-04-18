using System.Net;
using System.Net.Http;
using System.Text;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class FacebookOAuthServiceTests
{
  [Fact]
  public async Task ExchangeCodeForUserAsync_UsesConfiguredRedirectUri_AndReturnsUser()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{\"access_token\":\"token-123\"}", Encoding.UTF8, "application/json")
      },
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(
          "{\"id\":\"facebook-user\",\"name\":\"Uyen\",\"email\":\"uyen@example.com\",\"picture\":{\"data\":{\"url\":\"https://example.com/avatar.png\"}}}",
          Encoding.UTF8,
          "application/json")
      }
    ]);
    var service = new FacebookOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new FacebookOAuthSettings
      {
        AppId = "app-id",
        AppSecret = "app-secret",
        RedirectUri = "http://localhost:5173/auth/facebook/callback"
      }),
      NullLogger<FacebookOAuthService>.Instance);

    var result = await service.ExchangeCodeForUserAsync("auth-code");

    Assert.Equal("facebook-user", result.Subject);
    Assert.Equal("uyen@example.com", result.Email);
    Assert.True(result.EmailVerified);
    Assert.Equal("https://example.com/avatar.png", result.Picture);

    Assert.Collection(
      handler.Requests,
      request =>
      {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/oauth/access_token", request.RequestUri?.AbsoluteUri);
      },
      request =>
      {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/me", request.RequestUri?.AbsoluteUri);
      });

    Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A5173%2Fauth%2Ffacebook%2Fcallback", handler.Requests[0].RequestUri?.Query);
  }

  [Fact]
  public async Task ExchangeCodeForUserAsync_ThrowsFacebookOAuthExchangeException_WhenFacebookReturnsBadRequest()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("{\"error\":{\"message\":\"invalid code\"}}", Encoding.UTF8, "application/json")
      }
    ]);
    var service = new FacebookOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new FacebookOAuthSettings
      {
        AppId = "app-id",
        AppSecret = "app-secret",
        RedirectUri = "http://localhost:5173/auth/facebook/callback"
      }),
      NullLogger<FacebookOAuthService>.Instance);

    var exception = await Assert.ThrowsAsync<FacebookOAuthExchangeException>(() => service.ExchangeCodeForUserAsync("bad-code"));

    Assert.Equal("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.", exception.Message);
  }

  [Fact]
  public async Task ExchangeCodeForUserAsync_ThrowsFacebookOAuthExchangeException_WhenFacebookUserInfoFails()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{\"access_token\":\"token-123\"}", Encoding.UTF8, "application/json")
      },
      new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("{\"error\":{\"message\":\"invalid token\"}}", Encoding.UTF8, "application/json")
      }
    ]);
    var service = new FacebookOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new FacebookOAuthSettings
      {
        AppId = "app-id",
        AppSecret = "app-secret",
        RedirectUri = "http://localhost:5173/auth/facebook/callback"
      }),
      NullLogger<FacebookOAuthService>.Instance);

    var exception = await Assert.ThrowsAsync<FacebookOAuthExchangeException>(() => service.ExchangeCodeForUserAsync("bad-code"));

    Assert.Equal("Không thể xác minh đăng nhập Facebook. Vui lòng thử lại.", exception.Message);
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
