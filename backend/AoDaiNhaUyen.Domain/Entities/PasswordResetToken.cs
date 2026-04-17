namespace AoDaiNhaUyen.Domain.Entities;

public sealed class PasswordResetToken
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public required string Token { get; set; }
  public DateTime ExpiresAt { get; set; }
  public DateTime? UsedAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
}
