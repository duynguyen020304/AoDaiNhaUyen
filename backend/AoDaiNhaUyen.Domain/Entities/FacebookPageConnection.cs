using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class FacebookPageConnection : BaseEntity
{
  public required string PageId { get; set; }
  public string? PageName { get; set; }
  public required string EncryptedPageAccessToken { get; set; }
  public required string TokenLast4 { get; set; }
  public DateTimeOffset? ExpiresAt { get; set; }
  public DateTimeOffset? LastValidatedAt { get; set; }
}
