namespace AoDaiNhaUyen.Application.Options;

public sealed class HermesOutboxOptions
{
  public const string SectionName = "HermesOutbox";

  public bool Enabled { get; init; }
  public bool DryRun { get; init; } = true;
  public string RunnerName { get; init; } = "aodai-hermes-outbox-worker";
  public int BatchSize { get; init; } = 10;
  public int PollIntervalSeconds { get; init; } = 10;
  public int MaxAttempts { get; init; } = 5;
  public int LockTimeoutMinutes { get; init; } = 10;
  public int MaxPayloadBytes { get; init; } = 20_000;
  public decimal HighValueOrderThreshold { get; init; } = 5_000_000m;
  public int LowStockThreshold { get; init; } = 3;
  public string EventPath { get; init; } = "/v1/responses";
}
