using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class OrderAttributionServiceTests
{
  [Fact]
  public async Task CreateAsync_StoresFirstAndLastTouchSnapshot()
  {
    await using var dbContext = CreateDbContext();
    var userId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    dbContext.CustomerEvents.AddRange(
      new CustomerEvent
      {
        EventType = "viewed_product",
        UserId = userId,
        AnonymousSessionId = "session-1",
        Source = "google",
        Medium = "cpc",
        Campaign = "tet",
        OccurredAt = now.AddDays(-3)
      },
      new CustomerEvent
      {
        EventType = "promo_validated",
        UserId = userId,
        AnonymousSessionId = "session-1",
        Source = "facebook",
        Medium = "social",
        Campaign = "retargeting",
        OccurredAt = now.AddMinutes(-5)
      });
    dbContext.PromoCodes.Add(new PromoCode
    {
      Code = "TET10",
      DiscountType = "fixed",
      DiscountValue = 10000m,
      StartDate = now.AddDays(-1),
      EndDate = now.AddDays(1)
    });
    await dbContext.SaveChangesAsync();
    var order = new Order
    {
      Id = Guid.NewGuid(),
      OrderCode = "AD-ATTR",
      UserId = userId,
      RecipientName = "Uyen",
      RecipientPhone = "0900000000",
      Province = "TP.HCM",
      District = "1",
      AddressLine = "1 Nguyen Trai",
      Subtotal = 200000m,
      DiscountAmount = 10000m,
      ShippingFee = 0m,
      TotalAmount = 190000m,
      PlacedAt = now
    };
    var service = new OrderAttributionService(dbContext);

    await service.CreateAsync(order, "session-1", "tet10", 25000m);
    await dbContext.SaveChangesAsync();

    var attribution = await dbContext.OrderAttributions.SingleAsync();
    Assert.Equal(order.Id, attribution.OrderId);
    Assert.Equal("google", attribution.FirstTouchSource);
    Assert.Equal("cpc", attribution.FirstTouchMedium);
    Assert.Equal("tet", attribution.FirstTouchCampaign);
    Assert.Equal("facebook", attribution.LastTouchSource);
    Assert.Equal("social", attribution.LastTouchMedium);
    Assert.Equal("retargeting", attribution.LastTouchCampaign);
    Assert.Equal("TET10", attribution.PromoCode);
    Assert.Equal(190000m, attribution.AttributedRevenue);
    Assert.Equal(10000m, attribution.AttributedDiscount);
    Assert.Equal(25000m, attribution.AttributedShippingSubsidy);
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }
}
