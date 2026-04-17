namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Review
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public long ProductId { get; set; }
  public long? OrderItemId { get; set; }
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public bool IsVisible { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
  public Product Product { get; set; } = null!;
  public OrderItem? OrderItem { get; set; }
}
