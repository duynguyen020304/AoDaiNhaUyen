using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class OrderPromoCostSnapshot : BaseEntity
{
  public Guid OrderId { get; set; }
  public Guid? PromoCodeId { get; set; }
  public string? Code { get; set; }
  public string? DiscountType { get; set; }
  public decimal DiscountValue { get; set; }
  public decimal SubtotalBeforeDiscount { get; set; }
  public decimal DiscountAmount { get; set; }
  public decimal ShippingFeeBeforePromo { get; set; }
  public decimal ShippingFeeCharged { get; set; }
  public decimal ShippingSubsidy { get; set; }
  public decimal TotalAfterDiscount { get; set; }
  public decimal EstimatedCostOfGoods { get; set; }
  public decimal EstimatedGrossProfitBeforePromo { get; set; }
  public decimal EstimatedGrossProfitAfterPromo { get; set; }
  public decimal MarginLoss { get; set; }
  public Guid? AttributionCampaignId { get; set; }

  public Order Order { get; set; } = null!;
  public PromoCode? PromoCode { get; set; }
}
