using AoDaiNhaUyen.Application.DTOs.Promo;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class PromoService(AppDbContext dbContext) : IPromoService
{
  public async Task<PromoValidationResult> ValidateAsync(string code, decimal subtotal, CancellationToken cancellationToken = default)
  {
    return await CoreValidateAsync(code, subtotal, null, cancellationToken);
  }

  public async Task<PromoValidationResult> ApplyAsync(Guid orderId, string code, decimal subtotal, CancellationToken cancellationToken = default)
  {
    var result = await CoreValidateAsync(code, subtotal, orderId, cancellationToken);
    if (!result.IsValid)
    {
      return result;
    }

    return result;
  }

  private async Task<PromoValidationResult> CoreValidateAsync(string code, decimal subtotal, Guid? orderId, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(code))
    {
      return Invalid("empty_code", "Vui lòng nhập mã giảm giá.");
    }

    var normalizedCode = code.Trim().ToUpperInvariant();

    var promo = await dbContext.PromoCodes
      .AsNoTracking()
      .FirstOrDefaultAsync(p => p.Code == normalizedCode && p.IsActive && !p.IsDeleted, cancellationToken);

    if (promo is null)
    {
      return Invalid("invalid_code", "Mã giảm giá không tồn tại hoặc đã bị vô hiệu.");
    }

    var now = DateTime.UtcNow;
    if (now < promo.StartDate)
    {
      return Invalid("not_started", "Mã giảm giá chưa có hiệu lực.");
    }

    if (now > promo.EndDate)
    {
      return Invalid("expired", "Mã giảm giá đã hết hạn.");
    }

    if (promo.MaxUses > 0 && promo.CurrentUses >= promo.MaxUses)
    {
      return Invalid("max_uses_reached", "Mã giảm giá đã được sử dụng hết.");
    }

    if (subtotal < promo.MinOrderAmount)
    {
      return Invalid("min_order_not_met", $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0} VND để sử dụng mã này.");
    }

    var (discountAmount, label) = promo.DiscountType switch
    {
      "percentage" => (Math.Round(subtotal * promo.DiscountValue / 100m, 0), $"Giảm {promo.DiscountValue}%"),
      "fixed" => (promo.DiscountValue, $"Giảm {promo.DiscountValue:N0} VND"),
      _ => (0m, "Mã giảm giá")
    };

    if (orderId.HasValue)
    {
      var updatedRows = await dbContext.PromoCodes
        .Where(p => p.Id == promo.Id && p.IsActive && !p.IsDeleted && (p.MaxUses == 0 || p.CurrentUses < p.MaxUses))
        .ExecuteUpdateAsync(setters => setters
          .SetProperty(p => p.CurrentUses, p => p.CurrentUses + 1)
          .SetProperty(p => p.UpdatedAt, now), cancellationToken);

      if (updatedRows == 0)
      {
        return Invalid("max_uses_reached", "Mã giảm giá đã được sử dụng hết.");
      }

      var orderPromo = new OrderPromoCode
      {
        OrderId = orderId.Value,
        PromoCodeId = promo.Id,
        DiscountAmountApplied = discountAmount,
        CreatedAt = now
      };
      dbContext.OrderPromoCodes.Add(orderPromo);
    }

    return new PromoValidationResult(true, null, null, discountAmount, promo.FreeShipping, label);
  }

  private static PromoValidationResult Invalid(string errorCode, string message)
  {
    return new PromoValidationResult(false, errorCode, message, 0m, false, null);
  }
}
