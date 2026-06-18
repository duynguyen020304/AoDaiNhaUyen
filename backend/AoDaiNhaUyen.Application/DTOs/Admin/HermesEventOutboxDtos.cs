namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record HermesEventOutboxRequest
{
  public required string EventType { get; init; }
  public required string AggregateType { get; init; }
  public required string AggregateId { get; init; }
  public required string PayloadJson { get; init; }
  public string? CorrelationId { get; init; }
  public string? IdempotencyKey { get; init; }
  public DateTimeOffset? OccurredAt { get; init; }
  public DateTimeOffset? ScheduledAt { get; init; }
  public int? MaxAttempts { get; init; }
}

public sealed record HermesEventOutboxSearchRequest(
  int Page = 1,
  int PageSize = 20,
  string? Status = null,
  string? EventType = null,
  string? AggregateType = null,
  string? Q = null);

public sealed record HermesEventOutboxListItemResponse(
  Guid Id,
  string EventType,
  string AggregateType,
  string AggregateId,
  string Status,
  int Attempts,
  int MaxAttempts,
  string? LastError,
  string? CorrelationId,
  string? IdempotencyKey,
  DateTimeOffset OccurredAt,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? ProcessedAt,
  DateTime CreatedAt);

public sealed record HermesEventOutboxResponse(
  Guid Id,
  string EventType,
  string AggregateType,
  string AggregateId,
  string PayloadJson,
  string Status,
  int Attempts,
  int MaxAttempts,
  string? LastError,
  string? CorrelationId,
  string? IdempotencyKey,
  string? LockedBy,
  DateTimeOffset? LockedAt,
  DateTimeOffset OccurredAt,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? ProcessedAt,
  DateTime CreatedAt,
  DateTime UpdatedAt);
