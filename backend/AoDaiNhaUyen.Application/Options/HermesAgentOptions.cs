namespace AoDaiNhaUyen.Application.Options;

public sealed class HermesAgentOptions
{
  public const string SectionName = "Hermes";

  public string? ApiServerUrl { get; init; }
  public string? ApiServerKey { get; init; }
  public string RunnerName { get; init; } = "aodai-admin-hermes";
}
