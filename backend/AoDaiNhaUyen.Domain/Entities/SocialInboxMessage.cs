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

  /// <summary>
  /// S3 object key of the downloaded inbound image attachment (when applicable).
  /// Null when the message has no image or the download failed; in that case the
  /// agent must ask the customer to resend the photo. Populated only for
  /// Direction == "incoming" messages whose first image attachment was
  /// successfully fetched with the Page Access Token.
  /// </summary>
  public string? StoredImageKey { get; set; }

  /// <summary>
  /// Mime type of <see cref="StoredImageKey"/> (e.g. "image/jpeg").
  /// </summary>
  public string? StoredImageMimeType { get; set; }
}
