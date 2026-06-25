using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AoDaiNhaUyen.Domain.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Api.Authentication;

public sealed class HermesAdminApiKeyAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  IOptions<HermesAdminAuthOptions> hermesOptions,
  ILoggerFactory logger,
  UrlEncoder encoder)
  : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue(HermesAdminAuthOptions.HeaderName, out var headerValues))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    var providedKey = headerValues.FirstOrDefault();
    var configuredKey = hermesOptions.Value.AdminApiKey;
    if (string.IsNullOrWhiteSpace(providedKey) || string.IsNullOrWhiteSpace(configuredKey))
    {
      return Task.FromResult(AuthenticateResult.Fail("Hermes admin key is not configured."));
    }

    if (!FixedTimeEquals(providedKey, configuredKey))
    {
      return Task.FromResult(AuthenticateResult.Fail("Invalid Hermes admin key."));
    }

    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, HermesAgentIdentity.UserIdString),
      new(ClaimTypes.Name, "hermes_agent"),
      new(ClaimTypes.Role, RoleNames.Admin),
      new("agent", "hermes")
    };

    var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, Scheme.Name);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }

  private static bool FixedTimeEquals(string providedKey, string configuredKey)
  {
    var providedBytes = Encoding.UTF8.GetBytes(providedKey);
    var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
    return providedBytes.Length == configuredBytes.Length &&
      CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
  }
}
