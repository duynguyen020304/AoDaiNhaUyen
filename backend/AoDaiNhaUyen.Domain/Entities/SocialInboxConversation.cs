using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialInboxConversation : BaseEntity
{
  public string Platform { get; set; } = "facebook";
  public required string AccountId { get; set; }
  public string? AccountUsername { get; set; }
  public string? ProfileId { get; set; }
  public required string ConversationId { get; set; }
  public string? ParticipantId { get; set; }
  public string? ParticipantName { get; set; }
  public string? ParticipantPicture { get; set; }
  public string? LastMessage { get; set; }
  public DateTimeOffset? UpdatedTime { get; set; }
  public string? Status { get; set; }
  public int? UnreadCount { get; set; }
  public string? Url { get; set; }
  public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
  public string? RawJson { get; set; }
}
