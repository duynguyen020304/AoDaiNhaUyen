using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Shipment : BaseEntity
{
  public Guid OrderId { get; set; }
  public string? Carrier { get; set; }
  public string? TrackingNumber { get; set; }
  public string ShippingStatus { get; set; } = "pending";
  public DateTime? ShippedAt { get; set; }
  public DateTime? DeliveredAt { get; set; }


  public Order Order { get; set; } = null!;
}
