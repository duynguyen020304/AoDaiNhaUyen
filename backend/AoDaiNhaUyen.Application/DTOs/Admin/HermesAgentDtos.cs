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

public sealed record HermesStreamChunk(
  string Type,
  string Content,
  string? ToolName = null,
  string? ToolCallId = null);
