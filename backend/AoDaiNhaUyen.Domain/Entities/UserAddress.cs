using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserAddress : BaseEntity
{
  public Guid UserId { get; set; }
  public required string RecipientName { get; set; }
  public required string RecipientPhone { get; set; }
  public required string Province { get; set; }
  public required string District { get; set; }
  public string? Ward { get; set; }
  public required string AddressLine { get; set; }
  public bool IsDefault { get; set; }


  public User User { get; set; } = null!;
}
