using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class PromoCostService(AppDbContext dbContext) : IPromoCostService
{
  public async Task CreateOrderSnapshotAsync(
    Order order,
    string? promoCode,
    decimal normalShippingFee,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(promoCode))
    {
      return;
    }

    PromoCode? promo = null;
    var normalizedCode = promoCode.Trim().ToUpperInvariant();
    promo = await dbContext.PromoCodes.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);

    if (promo is null)
    {
      return;
    }

    var variantIds = order.Items.Select(x => x.VariantId).OfType<Guid>().Distinct().ToArray();
    var costPrices = await dbContext.ProductVariants
      .Where(x => variantIds.Contains(x.Id))
      .Select(x => new { x.Id, x.CostPrice })
      .ToDictionaryAsync(x => x.Id, x => x.CostPrice, cancellationToken);
    var estimatedCostOfGoods = order.Items.Sum(x => x.VariantId.HasValue && costPrices.TryGetValue(x.VariantId.Value, out var costPrice)
      ? costPrice * x.Quantity
      : 0m);
    var shippingSubsidy = Math.Max(0m, normalShippingFee - order.ShippingFee);
    var grossProfitBeforePromo = order.Subtotal - estimatedCostOfGoods;
    var grossProfitAfterPromo = order.Subtotal - order.DiscountAmount - estimatedCostOfGoods - shippingSubsidy;

    dbContext.OrderPromoCostSnapshots.Add(new OrderPromoCostSnapshot
    {
      OrderId = order.Id,
      PromoCodeId = promo?.Id,
      Code = promo?.Code,
      DiscountType = promo?.DiscountType,
      DiscountValue = promo?.DiscountValue ?? 0m,
      SubtotalBeforeDiscount = order.Subtotal,
      DiscountAmount = order.DiscountAmount,
      ShippingFeeBeforePromo = normalShippingFee,
      ShippingFeeCharged = order.ShippingFee,
      ShippingSubsidy = shippingSubsidy,
      TotalAfterDiscount = order.TotalAmount,
      EstimatedCostOfGoods = estimatedCostOfGoods,
      EstimatedGrossProfitBeforePromo = grossProfitBeforePromo,
      EstimatedGrossProfitAfterPromo = grossProfitAfterPromo,
      MarginLoss = grossProfitBeforePromo - grossProfitAfterPromo,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    });
  }

  public async Task<PromoPerformanceDto?> GetPromoPerformanceAsync(
    Guid promoCodeId,
    DateTime? from,
    DateTime? to,
    CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == promoCodeId, cancellationToken);
    if (promo is null) return null;

    var query = dbContext.OrderPromoCostSnapshots.AsNoTracking().Where(x => x.PromoCodeId == promoCodeId);
    if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
    if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

    var aggregate = await query
      .GroupBy(_ => 1)
      .Select(g => new
      {
        SnapshotCount = g.Count(),
        OrdersCount = g.Select(x => x.OrderId).Distinct().Count(),
        Subtotal = g.Sum(x => x.SubtotalBeforeDiscount),
        NetRevenue = g.Sum(x => x.TotalAfterDiscount),
        Discount = g.Sum(x => x.DiscountAmount),
        ShippingSubsidy = g.Sum(x => x.ShippingSubsidy),
        GrossProfitBefore = g.Sum(x => x.EstimatedGrossProfitBeforePromo),
        GrossProfitAfter = g.Sum(x => x.EstimatedGrossProfitAfterPromo),
        MarginLoss = g.Sum(x => x.MarginLoss)
      })
      .FirstOrDefaultAsync(cancellationToken);

    var snapshotCount = aggregate?.SnapshotCount ?? 0;
    var ordersCount = aggregate?.OrdersCount ?? 0;
    var netRevenue = aggregate?.NetRevenue ?? 0m;
    var discount = aggregate?.Discount ?? 0m;
    var shippingSubsidy = aggregate?.ShippingSubsidy ?? 0m;

    return new PromoPerformanceDto(
      promo.Id,
      promo.Code,
      snapshotCount,
      ordersCount,
      aggregate?.Subtotal ?? 0m,
      netRevenue,
      discount,
      shippingSubsidy,
      discount + shippingSubsidy,
      aggregate?.GrossProfitBefore ?? 0m,
      aggregate?.GrossProfitAfter ?? 0m,
      aggregate?.MarginLoss ?? 0m,
      ordersCount == 0 ? 0m : netRevenue / ordersCount);
  }
}
