using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserAccount : BaseEntity
{
  public Guid UserId { get; set; }
  public required string Provider { get; set; }
  public required string ProviderAccountId { get; set; }
  public string? PasswordHash { get; set; }
  public bool IsVerified { get; set; }


  public User User { get; set; } = null!;
}
