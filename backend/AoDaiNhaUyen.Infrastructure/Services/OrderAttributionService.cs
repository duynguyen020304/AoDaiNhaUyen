using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class OrderAttributionService(AppDbContext dbContext) : IOrderAttributionService
{
  public async Task CreateAsync(
    Order order,
    string? anonymousSessionId,
    string? promoCode,
    decimal normalShippingFee,
    CancellationToken cancellationToken = default)
  {
    var since = order.PlacedAt.AddDays(-30);
    var eventsQuery = dbContext.CustomerEvents.AsNoTracking()
      .Where(x => x.OccurredAt >= since && x.OccurredAt <= order.PlacedAt);

    if (!string.IsNullOrWhiteSpace(anonymousSessionId))
    {
      var sessionId = anonymousSessionId.Trim();
      eventsQuery = eventsQuery.Where(x => x.UserId == order.UserId || x.AnonymousSessionId == sessionId);
    }
    else
    {
      eventsQuery = eventsQuery.Where(x => x.UserId == order.UserId);
    }

    var attributedEvents = await eventsQuery
      .Where(x => x.Source != null || x.Medium != null || x.Campaign != null)
      .OrderBy(x => x.OccurredAt)
      .ToListAsync(cancellationToken);

    var firstTouch = attributedEvents.FirstOrDefault();
    var lastTouch = attributedEvents.LastOrDefault();
    var normalizedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim().ToUpperInvariant();
    var promo = normalizedPromoCode is null
      ? null
      : await dbContext.PromoCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == normalizedPromoCode, cancellationToken);
    var shippingSubsidy = Math.Max(0m, normalShippingFee - order.ShippingFee);

    dbContext.OrderAttributions.Add(new OrderAttribution
    {
      OrderId = order.Id,
      UserId = order.UserId,
      AnonymousSessionId = string.IsNullOrWhiteSpace(anonymousSessionId) ? null : anonymousSessionId.Trim(),
      FirstTouchSource = firstTouch?.Source,
      FirstTouchMedium = firstTouch?.Medium,
      FirstTouchCampaign = firstTouch?.Campaign,
      FirstTouchAt = firstTouch?.OccurredAt,
      LastTouchSource = lastTouch?.Source,
      LastTouchMedium = lastTouch?.Medium,
      LastTouchCampaign = lastTouch?.Campaign,
      LastTouchAt = lastTouch?.OccurredAt,
      PromoCodeId = promo?.Id,
      PromoCode = normalizedPromoCode,
      AttributedRevenue = order.TotalAmount,
      AttributedDiscount = order.DiscountAmount,
      AttributedShippingSubsidy = shippingSubsidy,
      MetadataJson = $"{{\"lookbackDays\":30,\"eventCount\":{attributedEvents.Count}}}",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    });
  }
}
