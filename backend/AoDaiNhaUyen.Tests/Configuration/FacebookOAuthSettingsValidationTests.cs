using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Configuration;

public sealed class FacebookOAuthSettingsValidationTests
{
  [Fact]
  public void AddBackendServices_RejectsMissingFacebookRedirectUri()
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
        ["JwtSettings:SecretKey"] = "abcdefghijklmnopqrstuvwxyz123456",
        ["JwtSettings:Issuer"] = "AoDaiNhaUyen.Api",
        ["JwtSettings:Audience"] = "AoDaiNhaUyen.Frontend",
        ["GoogleOAuth:ClientId"] = "client-id",
        ["GoogleOAuth:ClientSecret"] = "client-secret",
        ["GoogleOAuth:RedirectUri"] = "http://localhost:5173/auth/google/callback",
        ["FacebookOAuth:AppId"] = "app-id",
        ["FacebookOAuth:AppSecret"] = "app-secret",
        ["FacebookOAuth:RedirectUri"] = ""
      })
      .Build();

    var services = new ServiceCollection();

    services.AddBackendServices(configuration);
    using var provider = services.BuildServiceProvider();

    var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FacebookOAuthSettings>>().Value);
    Assert.Contains("RedirectUri", exception.Message, StringComparison.OrdinalIgnoreCase);
  }
}
