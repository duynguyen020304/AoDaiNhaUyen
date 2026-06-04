namespace AoDaiNhaUyen.Domain.Entities;

public sealed class CartItem
{
  public Guid Id { get; set; }
  public Guid CartId { get; set; }
  public Guid VariantId { get; set; }
  public int Quantity { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Cart Cart { get; set; } = null!;
  public ProductVariant Variant { get; set; } = null!;
}
