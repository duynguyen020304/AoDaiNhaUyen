using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialAccountConnection : BaseEntity
{
  public string Provider { get; set; } = "zernio";
  public string Platform { get; set; } = "facebook";
  public required string ZernioProfileId { get; set; }
  public required string ZernioAccountId { get; set; }
  public string? DisplayName { get; set; }
  public string? Username { get; set; }
  public string? AvatarUrl { get; set; }
  public DateTimeOffset? LastSyncedAt { get; set; }
  public DateTimeOffset? AutoReplyIgnoreBefore { get; set; }
  public string? MetadataJson { get; set; }
}
