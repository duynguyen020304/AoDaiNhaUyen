using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class EmailVerificationToken : BaseEntity
{
  public Guid UserId { get; set; }
  public required string Token { get; set; }
  public DateTime ExpiresAt { get; set; }
  public DateTime? UsedAt { get; set; }

  public User User { get; set; } = null!;
}
