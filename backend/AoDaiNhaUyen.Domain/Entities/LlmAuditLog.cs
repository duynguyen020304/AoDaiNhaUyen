using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class LlmAuditLog : BaseEntity
{
  public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
  public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
  public string? TraceId { get; set; }
  public Guid? ConversationId { get; set; }
  public Guid? ThreadId { get; set; }
  public Guid? MessageId { get; set; }
  public Guid? AdminActionId { get; set; }
  public Guid? UserGeneratedImageId { get; set; }
  public Guid? ActorUserId { get; set; }
  public string? ActorRole { get; set; }
  public string Source { get; set; } = "Unknown";
  public string? IpHash { get; set; }
  public string? UserAgentHash { get; set; }
  public string Provider { get; set; } = "Unknown";
  public string? Model { get; set; }
  public string Operation { get; set; } = "Unknown";
  public string? ActionType { get; set; }
  public string? ToolName { get; set; }
  public string? RiskLevel { get; set; }
  public bool RequiresConfirmation { get; set; }
  public Guid? ApprovedByUserId { get; set; }
  public DateTime? ApprovedAt { get; set; }
  public DateTime StartedAt { get; set; } = DateTime.UtcNow;
  public DateTime? CompletedAt { get; set; }
  public long? LatencyMs { get; set; }
  public int? PromptTokens { get; set; }
  public int? CompletionTokens { get; set; }
  public int? TotalTokens { get; set; }
  public decimal? EstimatedCost { get; set; }
  public string Status { get; set; } = "started";
  public string? ErrorCode { get; set; }
  public string? PromptPreviewRedacted { get; set; }
  public string? CompletionPreviewRedacted { get; set; }
  public string? InputMetadataJson { get; set; }
  public string? OutputMetadataJson { get; set; }
  public string? SafetyFlagsJson { get; set; }
  public string RedactionVersion { get; set; } = "v1";
  public DateTime RetainUntil { get; set; } = DateTime.UtcNow.AddDays(90);
}
