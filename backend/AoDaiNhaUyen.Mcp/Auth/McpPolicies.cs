using System.Security.Claims;

namespace AoDaiNhaUyen.Mcp.Auth;

public static class McpPolicies
{
  public const string Scheme = "McpApiKey";
  public const string ScopeClaim = "scope";

  public const string Read = "McpRead";
  public const string Write = "McpWrite";
  public const string Users = "McpUsers";
  public const string Roles = "McpRoles";

  public const string ReadScope = "mcp:read";
  public const string WriteScope = "mcp:write";
  public const string UsersScope = "mcp:users";
  public const string RolesScope = "mcp:roles";

  public static bool HasScope(this ClaimsPrincipal user, string scope) =>
    user.Claims.Any(c => c.Type == ScopeClaim && c.Value == scope);
}
