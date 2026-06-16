namespace AoDaiNhaUyen.Application.Options;

public sealed class AdminSeedOptions
{
  public const string SectionName = "AdminSeed";

  public string? Email { get; init; }
  public string? Password { get; init; }
}
