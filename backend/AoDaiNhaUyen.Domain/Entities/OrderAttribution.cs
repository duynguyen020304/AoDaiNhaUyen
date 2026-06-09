using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class OrderAttribution : BaseEntity
{
  public Guid OrderId { get; set; }
  public Guid UserId { get; set; }
  public string? AnonymousSessionId { get; set; }
  public string? FirstTouchSource { get; set; }
  public string? FirstTouchMedium { get; set; }
  public string? FirstTouchCampaign { get; set; }
  public DateTime? FirstTouchAt { get; set; }
  public string? LastTouchSource { get; set; }
  public string? LastTouchMedium { get; set; }
  public string? LastTouchCampaign { get; set; }
  public DateTime? LastTouchAt { get; set; }
  public Guid? PromoCodeId { get; set; }
  public string? PromoCode { get; set; }
  public decimal AttributedRevenue { get; set; }
  public decimal AttributedDiscount { get; set; }
  public decimal AttributedShippingSubsidy { get; set; }
  public string? MetadataJson { get; set; }

  public Order Order { get; set; } = null!;
  public User User { get; set; } = null!;
  public PromoCode? Promo { get; set; }
}
