using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Mcp.Auth;

public sealed class ApiKeyAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder,
  IOptions<McpAuthOptions> authOptions)
  : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  private const string ApiKeyHeader = "X-MCP-API-Key";
  private const string BearerPrefix = "Bearer ";

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var apiKey = ReadApiKey();
    if (string.IsNullOrWhiteSpace(apiKey))
      return Task.FromResult(AuthenticateResult.NoResult());

    foreach (var configuredKey in authOptions.Value.Keys)
    {
      if (!ApiKeyHasher.Verify(apiKey, configuredKey.Salt, configuredKey.Hash))
        continue;

      var claims = new List<Claim>
      {
        new(ClaimTypes.NameIdentifier, configuredKey.Id),
        new(ClaimTypes.Name, configuredKey.Id)
      };
      claims.AddRange(configuredKey.Scopes.Select(scope => new Claim(McpPolicies.ScopeClaim, scope)));

      var identity = new ClaimsIdentity(claims, Scheme.Name);
      var principal = new ClaimsPrincipal(identity);
      return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    return Task.FromResult(AuthenticateResult.Fail("Invalid MCP API key."));
  }

  private string? ReadApiKey()
  {
    if (Request.Headers.TryGetValue(ApiKeyHeader, out var headerValues))
      return headerValues.FirstOrDefault();

    var authorization = Request.Headers.Authorization.FirstOrDefault();
    return authorization?.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) == true
      ? authorization[BearerPrefix.Length..].Trim()
      : null;
  }
}
