using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class StockService(AppDbContext dbContext) : IStockService
{
  public async Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
  {
    // Uses parameterized raw SQL to atomically decrement stock.
    // The DbContext participates in the caller's transaction if one is active.
    var affected = await dbContext.Database.ExecuteSqlRawAsync(
      "UPDATE product_variants SET stock_qty = stock_qty - @p0, updated_at = NOW() WHERE id = @p1 AND stock_qty >= @p0",
      [quantity, variantId],
      cancellationToken);

    return affected > 0;
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
