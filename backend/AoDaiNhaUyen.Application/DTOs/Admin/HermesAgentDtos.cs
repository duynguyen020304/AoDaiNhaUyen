namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record HermesChatRequest
{
  public required string Message { get; init; }
  public string? ConversationId { get; init; }
}

public sealed record HermesHeartbeatRequest
{
  public required string RunnerName { get; init; }
  public required string Status { get; init; }
  public string? Model { get; init; }
  public string? GatewayStatus { get; init; }
  public int ActiveJobs { get; init; }
  public string? LastError { get; init; }
}

public sealed record HermesStatusResponse(
  string Status,
  string RunnerName,
  DateTimeOffset? LastHeartbeatAt,
  string? Model,
  string? GatewayStatus,
  int ActiveJobs,
  string? LastError,
  bool ApiServerConfigured);

public sealed record HermesRunSummaryResponse(
  Guid Id,
  string Status,
  string Trigger,
  string PromptPreview,
  string? ResultPreview,
  DateTimeOffset StartedAt,
  DateTimeOffset? CompletedAt,
  string? Error);

public sealed record HermesReportRequest
{
  public required string ReportType { get; init; }
  public required string Title { get; init; }
  public required string Summary { get; init; }
  public string Severity { get; init; } = "info";
  public string? PayloadJson { get; init; }
  public string? Source { get; init; }
  public string? CorrelationId { get; init; }
  public Guid? RunId { get; init; }
}

public sealed record HermesReportSearchRequest(
  int Page = 1,
  int PageSize = 20,
  string? Severity = null,
  string? Type = null,
  string? Status = null,
  string? Source = null,
  string? Q = null,
  DateTimeOffset? StartDate = null,
  DateTimeOffset? EndDate = null);

public sealed record HermesReportListItemResponse(
  Guid Id,
  string ReportType,
  string Severity,
  string Title,
  string SummaryPreview,
  string Source,
  string? CorrelationId,
  Guid? RunId,
  string Status,
  DateTime CreatedAt);

public sealed record HermesReportResponse(
  Guid Id,
  string ReportType,
  string Severity,
  string Title,
  string Summary,
  string? PayloadJson,
  string Source,
  string? CorrelationId,
  Guid? RunId,
  string Status,
  DateTime CreatedAt);

public sealed record HermesStreamChunk(
  string Type,
  string Content,
  string? ToolName = null,
  string? ToolCallId = null);
