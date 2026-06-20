using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialInboxMessage : BaseEntity
{
  public string Platform { get; set; } = "facebook";
  public required string AccountId { get; set; }
  public required string ConversationId { get; set; }
  public required string MessageId { get; set; }
  public string? SenderId { get; set; }
  public string? SenderName { get; set; }
  public string Direction { get; set; } = "incoming";
  public string? Text { get; set; }
  public DateTimeOffset? SentAt { get; set; }
  public string? AttachmentsJson { get; set; }
  public string? DeliveryStatus { get; set; }
  public bool? IsRead { get; set; }
  public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
  public string? RawJson { get; set; }
}
