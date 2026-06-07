using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class PromoCode : BaseEntity
{
  public required string Code { get; set; }
  public required string DiscountType { get; set; } = "percentage";
  public decimal DiscountValue { get; set; }
  public decimal MinOrderAmount { get; set; }
  public int MaxUses { get; set; }
  public int CurrentUses { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public bool FreeShipping { get; set; }

  public ICollection<OrderPromoCode> OrderPromoCodes { get; set; } = new List<OrderPromoCode>();
}
