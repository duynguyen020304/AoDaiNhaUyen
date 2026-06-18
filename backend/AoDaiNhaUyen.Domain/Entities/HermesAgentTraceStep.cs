using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesAgentTraceStep : BaseEntity
{
  public Guid? RunId { get; set; }
  public Guid? EventOutboxId { get; set; }
  public string Kind { get; set; } = "queued";
  public required string Title { get; set; }
  public required string Summary { get; set; }
  public string Status { get; set; } = "success";
  public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? CompletedAt { get; set; }
  public int? DurationMs { get; set; }
  public string? SafePayloadJson { get; set; }
  public string? Error { get; set; }

  public HermesRun? Run { get; set; }
  public HermesEventOutbox? EventOutbox { get; set; }
}
