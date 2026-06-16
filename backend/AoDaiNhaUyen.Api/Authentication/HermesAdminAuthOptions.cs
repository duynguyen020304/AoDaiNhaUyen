namespace AoDaiNhaUyen.Api.Authentication;

public sealed class HermesAdminAuthOptions
{
  public const string SectionName = "Hermes";
  public const string SchemeName = "HermesAdminKey";
  public const string HeaderName = "X-Hermes-Admin-Key";

  public string AdminApiKey { get; init; } = string.Empty;
}
