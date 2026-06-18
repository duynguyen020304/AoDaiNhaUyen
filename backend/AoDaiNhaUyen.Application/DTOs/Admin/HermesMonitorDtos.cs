namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record CreateHermesMonitorLinkRequest
{
  public string ScopeType { get; init; } = "event";
  public required string ScopeId { get; init; }
  public int? ExpiresInHours { get; init; }
}

public sealed record HermesMonitorLinkResponse(
  Guid Id,
  string Url,
  string Token,
  string ScopeType,
  string ScopeId,
  DateTimeOffset ExpiresAt,
  DateTimeOffset? RevokedAt,
  int AccessCount,
  DateTime CreatedAt);

public sealed record HermesMonitorSnapshotResponse(
  HermesMonitorLinkSummaryResponse Link,
  HermesMonitorEventSummaryResponse Event,
  IReadOnlyList<HermesMonitorRunSummaryResponse> Runs,
  IReadOnlyList<HermesMonitorStepResponse> TraceSteps,
  IReadOnlyList<HermesMonitorReportSummaryResponse> Reports,
  HermesMonitorHeartbeatSummaryResponse? Heartbeat,
  string ThinkingSummary,
  DateTimeOffset GeneratedAt);

public sealed record HermesMonitorLinkSummaryResponse(
  Guid Id,
  string ScopeType,
  string ScopeId,
  DateTimeOffset ExpiresAt,
  DateTimeOffset? RevokedAt,
  DateTimeOffset? LastAccessedAt,
  int AccessCount);

public sealed record HermesMonitorEventSummaryResponse(
  Guid Id,
  string EventType,
  string AggregateType,
  string AggregateId,
  string Status,
  int Attempts,
  int MaxAttempts,
  string? SafeError,
  string? CorrelationId,
  DateTimeOffset OccurredAt,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? ProcessedAt,
  DateTime CreatedAt);

public sealed record HermesMonitorRunSummaryResponse(
  Guid Id,
  string Status,
  string Trigger,
  string PromptSummary,
  string? ResultSummary,
  string? SafeError,
  DateTimeOffset StartedAt,
  DateTimeOffset? CompletedAt);

public sealed record HermesMonitorStepResponse(
  Guid Id,
  Guid? RunId,
  Guid? EventOutboxId,
  string Kind,
  string Title,
  string Summary,
  string Status,
  DateTimeOffset StartedAt,
  DateTimeOffset? CompletedAt,
  int? DurationMs,
  string? SafePayloadJson,
  string? SafeError);

public sealed record HermesMonitorReportSummaryResponse(
  Guid Id,
  string ReportType,
  string Severity,
  string Title,
  string Summary,
  string Source,
  string? CorrelationId,
  Guid? RunId,
  string Status,
  DateTime CreatedAt);

public sealed record HermesMonitorHeartbeatSummaryResponse(
  string RunnerName,
  string Status,
  string? Model,
  string? GatewayStatus,
  int ActiveJobs,
  string? SafeLastError,
  DateTimeOffset RecordedAt);
