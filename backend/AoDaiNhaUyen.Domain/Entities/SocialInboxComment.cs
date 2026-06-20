using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialInboxComment : BaseEntity
{
  public string Platform { get; set; } = "facebook";
  public required string AccountId { get; set; }
  public required string PostId { get; set; }
  public required string CommentId { get; set; }
  public string? ParentCommentId { get; set; }
  public string? AuthorId { get; set; }
  public string? AuthorName { get; set; }
  public string? AuthorUsername { get; set; }
  public string? AuthorPicture { get; set; }
  public bool AuthorIsOwner { get; set; }
  public string? Message { get; set; }
  public DateTimeOffset? CreatedTime { get; set; }
  public int LikeCount { get; set; }
  public int ReplyCount { get; set; }
  public string? Url { get; set; }
  public bool CanReply { get; set; }
  public bool CanDelete { get; set; }
  public bool CanHide { get; set; }
  public bool IsHidden { get; set; }
  public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
  public string? RawJson { get; set; }
}
