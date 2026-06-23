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

  /// <summary>
  /// When true, events claimed in one poll are sent to Hermes in a single batch
  /// HTTP call producing ONE comprehensive report covering all of them, instead
  /// of one call + one report per event. On any batch failure the worker falls
  /// back to per-event processing so retry/backoff/dead-letter guarantees stay
  /// intact. Default <c>false</c> — enable after testing.
  /// </summary>
  public bool BatchProcessingEnabled { get; init; }

  /// <summary>
  /// Maximum number of events in a single batch HTTP call. The claimed set is
  /// chunked if it exceeds this. Default 10.
  /// </summary>
  public int MaxBatchEvents { get; init; } = 10;

  /// <summary>
  /// Maximum total payload bytes (sum of every event's PayloadJson) for a single
  /// batch. Prevents LLM-context blowup. <c>0</c> = no limit. Default 500000.
  /// </summary>
  public int MaxBatchPayloadBytes { get; init; } = 500_000;

  /// <summary>
  /// Enables fan-out/fan-in batch processing: split claimed events into smaller
  /// concurrent Hermes sub-batches, then compress their partial reports into one
  /// final admin-visible report. Default false for safe rollout.
  /// </summary>
  public bool FanOutFanInEnabled { get; init; }

  /// <summary>Maximum events per fan-out Hermes sub-batch. Default 3.</summary>
  public int FanOutSubBatchSize { get; init; } = 3;

  /// <summary>Maximum concurrent Hermes sub-batch HTTP calls. Default 3.</summary>
  public int MaxParallelFanOutBatches { get; init; } = 3;

  /// <summary>When true, Vertex/Gemini compresses partial reports into one final report.</summary>
  public bool FanInCompressionEnabled { get; init; } = true;

  /// <summary>When true, use deterministic concatenation if AI compression fails.</summary>
  public bool FanInFallbackToConcatenation { get; init; } = true;

  /// <summary>Maximum characters from each partial report sent to the compressor.</summary>
  public int MaxPartialReportCharsForCompression { get; init; } = 6000;

  /// <summary>Retention days for persisted Hermes fan-out sub-batch checkpoints.</summary>
  public int FanOutSubBatchRetentionDays { get; init; } = 30;

  /// <summary>Minutes between cleanup passes for old Hermes fan-out sub-batch checkpoints.</summary>
  public int FanOutSubBatchCleanupIntervalMinutes { get; init; } = 1440;

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
