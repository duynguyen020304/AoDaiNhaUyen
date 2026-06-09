using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class CustomerEvent : BaseEntity
{
  public Guid? UserId { get; set; }
  public string? AnonymousSessionId { get; set; }
  public required string EventType { get; set; }
  public Guid? ProductId { get; set; }
  public Guid? ProductVariantId { get; set; }
  public Guid? OrderId { get; set; }
  public Guid? PromoCodeId { get; set; }
  public Guid? CampaignId { get; set; }
  public Guid? CampaignSendId { get; set; }
  public string? Source { get; set; }
  public string? Medium { get; set; }
  public string? Campaign { get; set; }
  public string? MetadataJson { get; set; }
  public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
  public string? IpHash { get; set; }
  public string? UserAgentHash { get; set; }

  public User? User { get; set; }
  public Product? Product { get; set; }
  public ProductVariant? ProductVariant { get; set; }
  public Order? Order { get; set; }
  public PromoCode? PromoCode { get; set; }
}
