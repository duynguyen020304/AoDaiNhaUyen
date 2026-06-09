using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class PromoCostServiceTests
{
  [Fact]
  public async Task CreateOrderSnapshotAsync_DoesNothing_WhenPromoCodeMissing()
  {
    await using var dbContext = CreateDbContext();
    var service = new PromoCostService(dbContext);
    var order = CreateOrder();

    await service.CreateOrderSnapshotAsync(order, null, 25000m);
    await dbContext.SaveChangesAsync();

    Assert.Empty(dbContext.OrderPromoCostSnapshots);
  }

  [Fact]
  public async Task CreateOrderSnapshotAsync_CreatesSnapshot_ForExistingPromoCode()
  {
    await using var dbContext = CreateDbContext();
    var variantId = Guid.NewGuid();
    dbContext.PromoCodes.Add(new PromoCode
    {
      Code = "SALE10",
      DiscountType = "fixed",
      DiscountValue = 10000m,
      StartDate = DateTime.UtcNow.AddDays(-1),
      EndDate = DateTime.UtcNow.AddDays(1)
    });
    dbContext.ProductVariants.Add(new ProductVariant
    {
      Id = variantId,
      ProductId = Guid.NewGuid(),
      Sku = "SKU-1",
      Price = 100000m,
      CostPrice = 40000m,
      StockQty = 10
    });
    await dbContext.SaveChangesAsync();

    var order = CreateOrder();
    order.Items.Add(new OrderItem
    {
      VariantId = variantId,
      ProductName = "Áo dài",
      UnitPrice = 100000m,
      Quantity = 2,
      LineTotal = 200000m
    });
    var service = new PromoCostService(dbContext);

    await service.CreateOrderSnapshotAsync(order, "sale10", 25000m);
    await dbContext.SaveChangesAsync();

    var snapshot = await dbContext.OrderPromoCostSnapshots.SingleAsync();
    Assert.Equal("SALE10", snapshot.Code);
    Assert.Equal(80000m, snapshot.EstimatedCostOfGoods);
    Assert.Equal(120000m, snapshot.EstimatedGrossProfitBeforePromo);
    Assert.Equal(85000m, snapshot.EstimatedGrossProfitAfterPromo);
    Assert.Equal(35000m, snapshot.MarginLoss);
  }

  private static Order CreateOrder()
  {
    return new Order
    {
      Id = Guid.NewGuid(),
      OrderCode = "AD-TEST",
      UserId = Guid.NewGuid(),
      RecipientName = "Uyen",
      RecipientPhone = "0900000000",
      Province = "TP.HCM",
      District = "1",
      AddressLine = "1 Nguyen Trai",
      Subtotal = 200000m,
      DiscountAmount = 10000m,
      ShippingFee = 0m,
      TotalAmount = 190000m
    };
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }
}
