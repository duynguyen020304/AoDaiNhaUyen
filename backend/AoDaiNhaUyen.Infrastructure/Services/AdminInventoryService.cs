using AoDaiNhaUyen.Application.Constants;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminInventoryService(
  AppDbContext dbContext,
  IAdminDashboardService dashboard,
  IFusionCacheService cache,
  ICacheKeyService cacheKeys,
  IOptions<FusionCacheSettings> cacheSettings) : IAdminInventoryService
{
  private TimeSpan GetCacheDuration(string key, int fallbackSeconds)
  {
    var settings = cacheSettings.Value;
    return settings.CacheDurations.TryGetValue(key, out var seconds)
      ? TimeSpan.FromSeconds(seconds)
      : TimeSpan.FromSeconds(fallbackSeconds);
  }

  public async Task<InventorySummary> GetInventorySummaryAsync(
    int threshold = 10, CancellationToken ct = default)
  {
    var products = await dbContext.Products
      .AsNoTracking()
      .Where(p => !p.IsDeleted && p.Status == "active")
      .Select(p => new
      {
        p.Id,
        p.Name,
        Variants = p.Variants.Select(v => new
        {
          v.Sku,
          v.Size,
          v.Color,
          v.StockQty
        }).ToList()
      })
      .ToListAsync(ct);

    var totalProducts = products.Count;
    var totalVariants = products.Sum(p => p.Variants.Count);

    var lowStockItems = new List<LowStockItem>();
    var outOfStockCount = 0;

    foreach (var p in products)
    {
      foreach (var v in p.Variants)
      {
        if (v.StockQty <= 0)
        {
          outOfStockCount++;
          lowStockItems.Add(new LowStockItem(
            p.Id, p.Name, v.Sku, v.Size, v.Color, v.StockQty));
        }
        else if (v.StockQty <= threshold)
        {
          lowStockItems.Add(new LowStockItem(
            p.Id, p.Name, v.Sku, v.Size, v.Color, v.StockQty));
        }
      }
    }

    // Sort by stock ascending (most critical first)
    lowStockItems = lowStockItems.OrderBy(i => i.StockQty).ToList();

    return new InventorySummary(
      totalProducts,
      totalVariants,
      lowStockItems.Count,
      outOfStockCount,
      lowStockItems);
  }

  public async Task<StoreHealthScore> GetStoreHealthScoreAsync(CancellationToken ct = default)
    => await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "store-health"),
      GetStoreHealthScoreCoreAsync,
      tags: [CacheTags.Dashboard, CacheTags.Products, CacheTags.Orders, CacheTags.Users],
      duration: GetCacheDuration("dashboard:store-health", 120),
      token: ct) ?? throw new InvalidOperationException("Không thể tải điểm sức khỏe cửa hàng.");

  private async Task<StoreHealthScore> GetStoreHealthScoreCoreAsync(CancellationToken ct)
  {
    var summary = await dashboard.GetSummaryAsync(ct);
    var ordersByStatus = await dashboard.GetOrdersByStatusAsync(ct);
    var revenue = await dashboard.GetRevenueAsync(7, ct);

    // Fulfillment rate: completed / (completed + cancelled + failed)
    var totalFinished = ordersByStatus.Completed + ordersByStatus.Cancelled + ordersByStatus.Returned;
    var fulfillmentRate = totalFinished > 0
      ? (int)Math.Round(100.0 * ordersByStatus.Completed / totalFinished)
      : 100;

    // Stock health: % of products with healthy stock (>10)
    var products = await dbContext.Products
      .AsNoTracking()
      .Where(p => !p.IsDeleted && p.Status == "active")
      .Select(p => p.Variants.Sum(v => v.StockQty))
      .ToListAsync(ct);

    var healthyStock = products.Count(s => s > 10);
    var stockHealth = products.Count > 0
      ? (int)Math.Round(100.0 * healthyStock / products.Count)
      : 100;

    // Revenue trend: compare last 3 days vs previous 3 days
    var points = revenue.Points.ToList();
    int revenueTrend = 50; // neutral
    if (points.Count >= 6)
    {
      var recent3 = points.TakeLast(3).Sum(p => p.Revenue);
      var prev3 = points.SkipLast(3).TakeLast(3).Sum(p => p.Revenue);
      if (prev3 > 0)
      {
        var growth = (double)((recent3 - prev3) / prev3 * 100);
        revenueTrend = Math.Clamp(50 + (int)Math.Round(growth), 0, 100);
      }
      else if (recent3 > 0)
      {
        revenueTrend = 80;
      }
    }

    // Customer satisfaction: placeholder (no review aggregation yet)
    var customerSatisfaction = 75;

    // Overall weighted score
    var overall = (int)Math.Round(
      fulfillmentRate * 0.30 +
      stockHealth * 0.25 +
      revenueTrend * 0.20 +
      customerSatisfaction * 0.15 +
      80 * 0.10); // response time placeholder

    var summaryText = overall switch
    {
      >= 85 => "Tuyệt vời! Cửa hàng hoạt động tốt.",
      >= 70 => "Tốt. Có vài điểm cần cải thiện.",
      >= 50 => "Trung bình. Cần chú ý một số vấn đề.",
      _ => "Cần cải thiện ngay. Có nhiều vấn đề cần giải quyết."
    };

    return new StoreHealthScore(
      Math.Clamp(overall, 0, 100),
      fulfillmentRate,
      stockHealth,
      revenueTrend,
      customerSatisfaction,
      summaryText);
  }

  private readonly AppDbContext dbContext = dbContext;
}
