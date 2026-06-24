using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialWebhookReceipt : BaseEntity
{
  public string Provider { get; set; } = "zernio";
  public string Platform { get; set; } = "facebook";
  public string EventType { get; set; } = string.Empty;
  public string? ExternalEventId { get; set; }
  public required string AccountId { get; set; }
  public required string ThreadId { get; set; }
  public required string MessageId { get; set; }
  public string Direction { get; set; } = "incoming";
  public DateTimeOffset? OccurredAt { get; set; }
  public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? ProcessedAt { get; set; }
  public string ReplyStatus { get; set; } = "pending";
  public string? ReplyMessageId { get; set; }
  public string? SkipReason { get; set; }
  public string? RawHash { get; set; }
}
