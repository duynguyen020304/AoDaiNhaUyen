using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class OrderPromoCode : BaseEntity
{
  public Guid OrderId { get; set; }
  public Guid PromoCodeId { get; set; }
  public decimal DiscountAmountApplied { get; set; }

  public Order Order { get; set; } = null!;
  public PromoCode PromoCode { get; set; } = null!;
}
