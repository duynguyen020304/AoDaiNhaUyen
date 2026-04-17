namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Order
{
  public long Id { get; set; }
  public required string OrderCode { get; set; }
  public long UserId { get; set; }
  public long? AddressId { get; set; }
  public required string RecipientName { get; set; }
  public required string RecipientPhone { get; set; }
  public required string Province { get; set; }
  public required string District { get; set; }
  public string? Ward { get; set; }
  public required string AddressLine { get; set; }
  public decimal Subtotal { get; set; }
  public decimal DiscountAmount { get; set; }
  public decimal ShippingFee { get; set; }
  public decimal TotalAmount { get; set; }
  public string OrderStatus { get; set; } = "pending";
  public string? Note { get; set; }
  public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ConfirmedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public DateTime? CancelledAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
  public UserAddress? Address { get; set; }
  public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
  public Payment? Payment { get; set; }
  public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
