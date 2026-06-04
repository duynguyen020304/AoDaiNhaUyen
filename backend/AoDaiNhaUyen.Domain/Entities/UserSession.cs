using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserSession : BaseEntity
{
  public Guid UserId { get; set; }
  public required string RefreshTokenHash { get; set; }
  public string? UserAgent { get; set; }
  public string? IpAddress { get; set; }
  public DateTime ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }


  public User User { get; set; } = null!;
}
