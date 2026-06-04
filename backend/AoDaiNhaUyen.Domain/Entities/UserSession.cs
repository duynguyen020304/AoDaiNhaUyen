namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserSession
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public required string RefreshTokenHash { get; set; }
  public string? UserAgent { get; set; }
  public string? IpAddress { get; set; }
  public DateTime ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
}
