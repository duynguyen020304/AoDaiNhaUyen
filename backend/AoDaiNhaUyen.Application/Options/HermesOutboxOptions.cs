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
  public int MaxPayloadBytes { get; init; } = 0;
  public decimal HighValueOrderThreshold { get; init; } = 5_000_000m;
  public int LowStockThreshold { get; init; } = 3;
  public string EventPath { get; init; } = "/v1/responses";

  /// <summary>
  /// Master switch for autonomous execution of admin mutations by the Hermes
  /// agent (chat + cron paths). Default <c>true</c> — the gateway is the real
  /// gate; this is a cheap server-side kill switch that returns 403 for agent
  /// mutations when flipped off via env <c>HermesOutbox__AutoExecuteEnabled=false</c>.
  /// Event/outbox path is unaffected (always analysis-only — payloads are untrusted).
  /// </summary>
  public bool AutoExecuteEnabled { get; init; } = true;

  /// <summary>
  /// Optional temporal kill switch. When set to a future UTC timestamp, agent
  /// mutations are rejected until that moment passes. Useful for cooling off
  /// without losing the env flag. Set via <c>HermesOutbox__SuspendUntil</c>.
  /// </summary>
  public DateTimeOffset? SuspendUntil { get; init; }
}
