namespace AoDaiNhaUyen.Domain.Entities;

public sealed class UserAddress
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public required string RecipientName { get; set; }
  public required string RecipientPhone { get; set; }
  public required string Province { get; set; }
  public required string District { get; set; }
  public string? Ward { get; set; }
  public required string AddressLine { get; set; }
  public bool IsDefault { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
}
