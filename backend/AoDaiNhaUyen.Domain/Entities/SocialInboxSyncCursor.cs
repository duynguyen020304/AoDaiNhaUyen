using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialInboxSyncCursor : BaseEntity
{
  public required string Resource { get; set; }
  public string Platform { get; set; } = "facebook";
  public string AccountId { get; set; } = string.Empty;
  public string ProfileId { get; set; } = string.Empty;
  public string? Cursor { get; set; }
  public DateTimeOffset? LastSuccessAt { get; set; }
  public string? LastError { get; set; }
}
