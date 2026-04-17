namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserAccount
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public required string Provider { get; set; }
  public required string ProviderAccountId { get; set; }
  public string? PasswordHash { get; set; }
  public bool IsVerified { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
}
