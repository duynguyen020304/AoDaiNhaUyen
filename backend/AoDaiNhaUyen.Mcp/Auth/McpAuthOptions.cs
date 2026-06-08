namespace AoDaiNhaUyen.Mcp.Auth;

public sealed class McpAuthOptions
{
  public const string SectionName = "McpAuth";

  public List<McpApiKeyOptions> Keys { get; init; } = [];
}

public sealed class McpApiKeyOptions
{
  public string Id { get; init; } = string.Empty;

  public string Hash { get; init; } = string.Empty;

  public string Salt { get; init; } = string.Empty;

  public string[] Scopes { get; init; } = [];
}
