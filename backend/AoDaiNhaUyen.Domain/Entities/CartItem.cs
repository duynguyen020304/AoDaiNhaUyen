namespace AoDaiNhaUyen.Domain.Entities;

public sealed class CartItem
{
  public long Id { get; set; }
  public long CartId { get; set; }
  public long VariantId { get; set; }
  public int Quantity { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Cart Cart { get; set; } = null!;
  public ProductVariant Variant { get; set; } = null!;
}
