using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class CartItem : BaseEntity
{
  public Guid CartId { get; set; }
  public Guid VariantId { get; set; }
  public int Quantity { get; set; }

  public Cart Cart { get; set; } = null!;
  public ProductVariant Variant { get; set; } = null!;
}
