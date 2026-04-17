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

public sealed class GoogleOAuthServiceTests
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
          "{\"sub\":\"google-user\",\"email\":\"uyen@example.com\",\"email_verified\":true,\"name\":\"Uyen\",\"picture\":\"https://example.com/avatar.png\"}",
          Encoding.UTF8,
          "application/json")
      }
    ]);
    var service = new GoogleOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new GoogleOAuthSettings
      {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUri = "http://localhost:5173/auth/google/callback"
      }),
      NullLogger<GoogleOAuthService>.Instance);

    var result = await service.ExchangeCodeForUserAsync("auth-code");

    Assert.Equal("google-user", result.Subject);
    Assert.Equal("uyen@example.com", result.Email);
    Assert.True(result.EmailVerified);

    Assert.Collection(
      handler.Requests,
      request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://oauth2.googleapis.com/token", request.RequestUri?.ToString());
      },
      request =>
      {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://openidconnect.googleapis.com/v1/userinfo", request.RequestUri?.ToString());
      });

    var formBody = await handler.Requests[0].Content!.ReadAsStringAsync();
    Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A5173%2Fauth%2Fgoogle%2Fcallback", formBody);
  }

  [Fact]
  public async Task ExchangeCodeForUserAsync_ThrowsGoogleOAuthExchangeException_WhenGoogleReturnsBadRequest()
  {
    var handler = new StubHttpMessageHandler([
      new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("{\"error\":\"invalid_grant\"}", Encoding.UTF8, "application/json")
      }
    ]);
    var service = new GoogleOAuthService(
      new StubHttpClientFactory(new HttpClient(handler)),
      Options.Create(new GoogleOAuthSettings
      {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUri = "http://localhost:5173/auth/google/callback"
      }),
      NullLogger<GoogleOAuthService>.Instance);

    var exception = await Assert.ThrowsAsync<GoogleOAuthExchangeException>(() => service.ExchangeCodeForUserAsync("bad-code"));

    Assert.Equal("Không thể xác minh đăng nhập Google. Vui lòng thử lại.", exception.Message);
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
