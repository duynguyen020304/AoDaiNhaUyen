using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
  public Guid OrderId { get; set; }
  public Guid? ProductId { get; set; }
  public Guid? VariantId { get; set; }
  public required string ProductName { get; set; }
  public string? Sku { get; set; }
  public string? Size { get; set; }
  public string? Color { get; set; }
  public decimal UnitPrice { get; set; }
  public int Quantity { get; set; }
  public decimal LineTotal { get; set; }
  public bool IsCustomTailoring { get; set; }
  public Guid? MeasurementProfileId { get; set; }
  public string? CustomMeasurementsJson { get; set; }
  public string? Note { get; set; }

  public Order Order { get; set; } = null!;
  public Product? Product { get; set; }
  public ProductVariant? Variant { get; set; }
  public MeasurementProfile? MeasurementProfile { get; set; }
}
