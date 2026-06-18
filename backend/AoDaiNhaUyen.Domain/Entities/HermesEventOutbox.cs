using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesEventOutbox : BaseEntity
{
  public string EventType { get; set; } = string.Empty;
  public string AggregateType { get; set; } = string.Empty;
  public string AggregateId { get; set; } = string.Empty;
  public string PayloadJson { get; set; } = "{}";
  public string Status { get; set; } = "pending";
  public int Attempts { get; set; }
  public int MaxAttempts { get; set; } = 5;
  public string? LastError { get; set; }
  public string? CorrelationId { get; set; }
  public string? IdempotencyKey { get; set; }
  public string? LockedBy { get; set; }
  public DateTimeOffset? LockedAt { get; set; }
  public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset ScheduledAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? ProcessedAt { get; set; }
}
