using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialAutoReplyBatch : BaseEntity
{
  public string Platform { get; set; } = "facebook";
  public required string AccountId { get; set; }
  public required string ConversationId { get; set; }
  public string Status { get; set; } = "pending";
  public DateTimeOffset WindowStartedAt { get; set; }
  public DateTimeOffset WindowEndsAt { get; set; }
  public DateTimeOffset LastMessageAt { get; set; }
  public string MessageIdsJson { get; set; } = "[]";
  public int MessageCount { get; set; }
  public string? ReplyMessageId { get; set; }
  public string? LastError { get; set; }
  public string? LockedBy { get; set; }
  public DateTimeOffset? LockedAt { get; set; }
  public DateTimeOffset? ProcessedAt { get; set; }
}
