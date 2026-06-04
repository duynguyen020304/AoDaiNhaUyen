namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Shipment
{
  public Guid Id { get; set; }
  public Guid OrderId { get; set; }
  public string? Carrier { get; set; }
  public string? TrackingNumber { get; set; }
  public string ShippingStatus { get; set; } = "pending";
  public DateTime? ShippedAt { get; set; }
  public DateTime? DeliveredAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Order Order { get; set; } = null!;
}
