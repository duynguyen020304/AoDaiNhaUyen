using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Cart : BaseEntity
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
  public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
