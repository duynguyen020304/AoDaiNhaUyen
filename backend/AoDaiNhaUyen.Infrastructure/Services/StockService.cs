using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class StockService(
  AppDbContext dbContext,
  IHermesEventOutboxPublisher hermesEvents,
  Microsoft.Extensions.Options.IOptions<HermesOutboxOptions> options) : IStockService
{
  public async Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
  {
    // Uses parameterized raw SQL to atomically decrement stock.
    // The DbContext participates in the caller's transaction if one is active.
    var affected = await dbContext.Database.ExecuteSqlRawAsync(
      "UPDATE product_variants SET stock_qty = stock_qty - @p0, updated_at = NOW() WHERE id = @p1 AND stock_qty >= @p0",
      [quantity, variantId],
      cancellationToken);

    if (affected <= 0) return false;

    var threshold = Math.Max(0, options.Value.LowStockThreshold);
    var variant = await dbContext.ProductVariants
      .AsNoTracking()
      .Include(v => v.Product)
      .Where(v => v.Id == variantId)
      .Select(v => new { v.Id, v.Sku, v.StockQty, v.ProductId, ProductName = v.Product.Name })
      .FirstOrDefaultAsync(cancellationToken);

    if (variant is not null && variant.StockQty <= threshold)
    {
      await hermesEvents.EnqueueAdminInventoryEventAsync(
        "low_stock",
        variant.Id,
        new { variantId = variant.Id, variant.ProductId, variant.Sku, variant.ProductName, stockQty = variant.StockQty, threshold },
        $"low_stock:Inventory:{variant.Id:N}:{variant.StockQty}:{DateTime.UtcNow.Date.Ticks}",
        cancellationToken);
    }

    return true;
  }

  public async Task ReleaseStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
  {
    await dbContext.Database.ExecuteSqlRawAsync(
      "UPDATE product_variants SET stock_qty = stock_qty + @p0, updated_at = NOW() WHERE id = @p1",
      [quantity, variantId],
      cancellationToken);
  }

  public async Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(int threshold = 5, CancellationToken cancellationToken = default)
  {
    return await dbContext.ProductVariants
      .AsNoTracking()
      .Where(v => v.StockQty <= threshold && v.Status == "active" && !v.IsDeleted)
      .OrderBy(v => v.StockQty)
      .Select(v => new LowStockAlertDto(
        v.Id,
        v.Product.Name,
        v.VariantName,
        v.Size,
        v.Color,
        v.Sku,
        v.StockQty))
      .ToListAsync(cancellationToken);
  }
}
