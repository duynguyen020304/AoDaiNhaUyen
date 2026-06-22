using AoDaiNhaUyen.Application.Constants;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminDashboardService(
  AppDbContext dbContext,
  IImageVisibilityService imageVisibilityService,
  IFusionCacheService cache,
  ICacheKeyService cacheKeys,
  IOptions<FusionCacheSettings> cacheSettings) : IAdminDashboardService
{
  private TimeSpan GetCacheDuration(string key, int fallbackSeconds)
  {
    var settings = cacheSettings.Value;
    return settings.CacheDurations.TryGetValue(key, out var seconds)
      ? TimeSpan.FromSeconds(seconds)
      : TimeSpan.FromSeconds(fallbackSeconds);
  }

  public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    => await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "summary"),
      GetSummaryCoreAsync,
      tags: [CacheTags.Dashboard],
      duration: GetCacheDuration("dashboard:summary", 60),
      token: ct) ?? throw new InvalidOperationException("Không thể tải tổng quan dashboard.");

  private async Task<DashboardSummaryDto> GetSummaryCoreAsync(CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var currentPeriodStart = now.AddDays(-30);
    var previousPeriodStart = now.AddDays(-60);

    // Current period revenue (orders with payment)
    var currentRevenue = await dbContext.Orders
      .AsNoTracking()
      .Where(o => o.Payment != null && !o.IsDeleted && o.CreatedAt >= currentPeriodStart && o.CreatedAt < now)
      .SumAsync(o => o.TotalAmount, ct);

    // Previous period revenue
    var previousRevenue = await dbContext.Orders
      .AsNoTracking()
      .Where(o => o.Payment != null && !o.IsDeleted && o.CreatedAt >= previousPeriodStart && o.CreatedAt < currentPeriodStart)
      .SumAsync(o => o.TotalAmount, ct);

    // Current period orders
    var currentOrders = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted && o.CreatedAt >= currentPeriodStart && o.CreatedAt < now)
      .CountAsync(ct);

    // Previous period orders
    var previousOrders = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted && o.CreatedAt >= previousPeriodStart && o.CreatedAt < currentPeriodStart)
      .CountAsync(ct);

    // Current active users
    var currentUsers = await dbContext.Users
      .AsNoTracking()
      .Where(u => u.Status == "active" && !u.IsDeleted && u.CreatedAt < now)
      .CountAsync(ct);

    // Users at start of current period
    var previousUsers = await dbContext.Users
      .AsNoTracking()
      .Where(u => u.Status == "active" && !u.IsDeleted && u.CreatedAt < currentPeriodStart)
      .CountAsync(ct);

    // Current products
    var currentProducts = await dbContext.Products
      .AsNoTracking()
      .Where(p => !p.IsDeleted && p.CreatedAt < now)
      .CountAsync(ct);

    // Products at start of current period
    var previousProducts = await dbContext.Products
      .AsNoTracking()
      .Where(p => !p.IsDeleted && p.CreatedAt < currentPeriodStart)
      .CountAsync(ct);

    static double CalcGrowth(decimal current, decimal previous) =>
      previous == 0 ? (current > 0 ? 100.0 : 0.0) : (double)((current - previous) / previous * 100);

    return new DashboardSummaryDto(
      TotalRevenue: currentRevenue,
      TotalOrders: currentOrders,
      TotalUsers: currentUsers,
      TotalProducts: currentProducts,
      RevenueGrowth: Math.Round(CalcGrowth(currentRevenue, previousRevenue), 1),
      OrdersGrowth: Math.Round(CalcGrowth(currentOrders, previousOrders), 1),
      UsersGrowth: Math.Round(CalcGrowth(currentUsers, previousUsers), 1),
      ProductsGrowth: Math.Round(CalcGrowth(currentProducts, previousProducts), 1));
  }

  public async Task<RevenueDataDto> GetRevenueAsync(int periodDays, CancellationToken ct = default)
  {
    var period = Math.Clamp(periodDays, 1, 365);
    return await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "revenue", period.ToString()),
      token => GetRevenueCoreAsync(period, token),
      tags: [CacheTags.Dashboard, CacheTags.Orders],
      duration: GetCacheDuration("dashboard:revenue", 120),
      token: ct) ?? throw new InvalidOperationException("Không thể tải dữ liệu doanh thu.");
  }

  private async Task<RevenueDataDto> GetRevenueCoreAsync(int period, CancellationToken ct)
  {
    var start = DateTime.UtcNow.Date.AddDays(-period + 1);

    var raw = await dbContext.Orders
      .AsNoTracking()
      .Where(o => o.Payment != null && !o.IsDeleted && o.CreatedAt >= start)
      .GroupBy(o => o.CreatedAt.Date)
      .Select(g => new
      {
        Date = g.Key,
        Revenue = g.Sum(o => o.TotalAmount),
        Orders = g.Count()
      })
      .OrderBy(x => x.Date)
      .ToListAsync(ct);

    var points = new List<RevenuePointDto>();
    for (var d = start; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
    {
      var match = raw.FirstOrDefault(x => x.Date == d);
      points.Add(new RevenuePointDto(d, match?.Revenue ?? 0, match?.Orders ?? 0));
    }

    return new RevenueDataDto(
      Period: period <= 7 ? "daily" : period <= 30 ? "daily" : "weekly",
      Points: points);
  }

  public async Task<OrderStatusDistributionDto> GetOrdersByStatusAsync(CancellationToken ct = default)
    => await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "orders-by-status"),
      GetOrdersByStatusCoreAsync,
      tags: [CacheTags.Dashboard, CacheTags.Orders],
      duration: GetCacheDuration("dashboard:orders-by-status", 60),
      token: ct) ?? throw new InvalidOperationException("Không thể tải thống kê trạng thái đơn hàng.");

  private async Task<OrderStatusDistributionDto> GetOrdersByStatusCoreAsync(CancellationToken ct)
  {
    var groups = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted)
      .GroupBy(o => o.OrderStatus)
      .Select(g => new { Status = g.Key, Count = g.Count() })
      .ToListAsync(ct);

    int Get(string status) => groups.FirstOrDefault(g => g.Status == status)?.Count ?? 0;

    return new OrderStatusDistributionDto(
      Pending: Get("pending"),
      Confirmed: Get("confirmed"),
      Processing: Get("processing"),
      Shipping: Get("shipping"),
      Completed: Get("completed"),
      Cancelled: Get("cancelled"),
      Returned: Get("returned"));
  }

  public async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int limit, CancellationToken ct = default)
  {
    var normalizedLimit = Math.Clamp(limit, 1, 50);
    return await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "recent-orders", normalizedLimit.ToString()),
      token => GetRecentOrdersCoreAsync(normalizedLimit, token),
      tags: [CacheTags.Dashboard, CacheTags.Orders],
      duration: GetCacheDuration("dashboard:recent-orders", 30),
      token: ct) ?? [];
  }

  private async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersCoreAsync(int limit, CancellationToken ct)
  {
    limit = Math.Clamp(limit, 1, 50);

    var orders = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted)
      .OrderByDescending(o => o.CreatedAt)
      .Take(limit)
      .Select(o => new RecentOrderDto(
        o.Id,
        o.OrderCode,
        o.User.FullName,
        o.TotalAmount,
        o.OrderStatus,
        new DateTimeOffset(o.CreatedAt, TimeSpan.Zero)))
      .ToListAsync(ct);

    return orders;
  }

  public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int limit, CancellationToken ct = default)
  {
    var normalizedLimit = Math.Clamp(limit, 1, 20);
    return await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "top-products", normalizedLimit.ToString()),
      token => GetTopProductsCoreAsync(normalizedLimit, token),
      tags: [CacheTags.Dashboard, CacheTags.Products, CacheTags.Orders],
      duration: GetCacheDuration("dashboard:top-products", 120),
      token: ct) ?? [];
  }

  private async Task<IReadOnlyList<TopProductDto>> GetTopProductsCoreAsync(int limit, CancellationToken ct)
  {
    limit = Math.Clamp(limit, 1, 20);

    var top = await dbContext.OrderItems
      .AsNoTracking()
      .Where(oi => !oi.Order.IsDeleted)
      .GroupBy(oi => new { oi.ProductId, oi.ProductName })
      .Select(g => new
      {
        g.Key.ProductId,
        g.Key.ProductName,
        SoldCount = g.Sum(oi => oi.Quantity),
        Revenue = g.Sum(oi => oi.LineTotal)
      })
      .OrderByDescending(x => x.Revenue)
      .Take(limit)
      .ToListAsync(ct);

    // Get images for products that still exist
    var productIds = top.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList();
    var images = await dbContext.ProductImages
      .AsNoTracking()
      .Where(img => productIds.Contains(img.ProductId) && img.SortOrder == 1 && !img.IsDeleted)
      .GroupBy(img => img.ProductId)
      .Select(g => new
      {
        ProductId = g.Key,
        g.First().ImageUrl,
        g.First().IsPublic,
        g.First().PublicObjectKey
      })
      .ToListAsync(ct);

    var imageLookup = new Dictionary<Guid, string?>();
    foreach (var image in images)
    {
      imageLookup[image.ProductId] = await imageVisibilityService.ResolveUrlAsync(
        image.ImageUrl,
        image.IsPublic,
        image.PublicObjectKey,
        ct);
    }

    return top.Select(x => new TopProductDto(
      x.ProductId,
      x.ProductName,
      x.ProductId.HasValue && imageLookup.TryGetValue(x.ProductId.Value, out var url) ? url : null,
      x.SoldCount,
      x.Revenue)).ToList();
  }

  public async Task<UserGrowthDataDto> GetUserGrowthAsync(int periodDays, CancellationToken ct = default)
  {
    var period = Math.Clamp(periodDays, 1, 365);
    return await cache.GetOrSetAsync(
      cacheKeys.BuildAdminKey("dashboard", "user-growth", period.ToString()),
      token => GetUserGrowthCoreAsync(period, token),
      tags: [CacheTags.Dashboard, CacheTags.Users],
      duration: GetCacheDuration("dashboard:user-growth", 120),
      token: ct) ?? throw new InvalidOperationException("Không thể tải dữ liệu tăng trưởng người dùng.");
  }

  private async Task<UserGrowthDataDto> GetUserGrowthCoreAsync(int period, CancellationToken ct)
  {
    var start = DateTime.UtcNow.Date.AddDays(-period + 1);

    var raw = await dbContext.Users
      .AsNoTracking()
      .Where(u => !u.IsDeleted && u.CreatedAt >= start)
      .GroupBy(u => u.CreatedAt.Date)
      .Select(g => new { Date = g.Key, NewUsers = g.Count() })
      .OrderBy(x => x.Date)
      .ToListAsync(ct);

    // Count users created before the start to get baseline
    var baseline = await dbContext.Users
      .AsNoTracking()
      .Where(u => !u.IsDeleted && u.CreatedAt < start)
      .CountAsync(ct);

    var points = new List<UserGrowthPointDto>();
    var runningTotal = baseline;
    for (var d = start; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
    {
      var match = raw.FirstOrDefault(x => x.Date == d);
      runningTotal += match?.NewUsers ?? 0;
      points.Add(new UserGrowthPointDto(d, match?.NewUsers ?? 0, runningTotal));
    }

    return new UserGrowthDataDto(points);
  }

  // --- Explicit date-range queries (no cache — these are arbitrary ranges) ---

  public async Task<RevenueDataDto> GetRevenueByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default)
  {
    var (start, end) = NormalizeRange(startDateUtc, endDateUtc);
    var endExclusive = end.AddDays(1);

    var raw = await dbContext.Orders
      .AsNoTracking()
      .Where(o => o.Payment != null && !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt < endExclusive)
      .GroupBy(o => o.CreatedAt.Date)
      .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
      .OrderBy(x => x.Date)
      .ToListAsync(ct);

    var points = new List<RevenuePointDto>();
    for (var d = start; d <= end; d = d.AddDays(1))
    {
      var match = raw.FirstOrDefault(x => x.Date == d);
      points.Add(new RevenuePointDto(d, match?.Revenue ?? 0, match?.Orders ?? 0));
    }
    return new RevenueDataDto("daily", points);
  }

  public async Task<OrderStatusDistributionDto> GetOrdersByStatusByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default)
  {
    var (start, end) = NormalizeRange(startDateUtc, endDateUtc);
    var endExclusive = end.AddDays(1);

    var groups = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt < endExclusive)
      .GroupBy(o => o.OrderStatus)
      .Select(g => new { Status = g.Key, Count = g.Count() })
      .ToListAsync(ct);

    int Get(string status) => groups.FirstOrDefault(g => g.Status == status)?.Count ?? 0;
    return new OrderStatusDistributionDto(
      Pending: Get("pending"),
      Confirmed: Get("confirmed"),
      Processing: Get("processing"),
      Shipping: Get("shipping"),
      Completed: Get("completed"),
      Cancelled: Get("cancelled"),
      Returned: Get("returned"));
  }

  public async Task<IReadOnlyList<TopProductDto>> GetTopProductsByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken ct = default)
  {
    var (start, end) = NormalizeRange(startDateUtc, endDateUtc);
    var endExclusive = end.AddDays(1);
    var cappedLimit = Math.Clamp(limit, 1, 50);

    var top = await dbContext.OrderItems
      .AsNoTracking()
      .Where(oi => !oi.Order.IsDeleted && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < endExclusive)
      .GroupBy(oi => new { oi.ProductId, oi.ProductName })
      .Select(g => new
      {
        g.Key.ProductId,
        g.Key.ProductName,
        SoldCount = g.Sum(oi => oi.Quantity),
        Revenue = g.Sum(oi => oi.LineTotal)
      })
      .OrderByDescending(x => x.Revenue)
      .Take(cappedLimit)
      .ToListAsync(ct);

    return top.Select(x => new TopProductDto(x.ProductId, x.ProductName, null, x.SoldCount, x.Revenue)).ToList();
  }

  public async Task<DashboardRangeMetricsDto> GetRangeMetricsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default)
  {
    var (start, end) = NormalizeRange(startDateUtc, endDateUtc);
    var endExclusive = end.AddDays(1);

    var allOrders = await dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt < endExclusive)
      .Select(o => new { o.OrderStatus, o.TotalAmount, HasPayment = o.Payment != null })
      .ToListAsync(ct);

    var totalOrders = allOrders.Count;
    var paidOrders = allOrders.Count(o => o.HasPayment);
    var cancelledOrders = allOrders.Count(o => o.OrderStatus == "cancelled");
    var totalRevenue = allOrders.Sum(o => o.TotalAmount);
    var paidRevenue = allOrders.Where(o => o.HasPayment).Sum(o => o.TotalAmount);
    var aov = paidOrders > 0 ? paidRevenue / paidOrders : 0m;

    return new DashboardRangeMetricsDto(start, end, totalOrders, paidOrders, cancelledOrders, totalRevenue, paidRevenue, aov);
  }

  private static (DateTime Start, DateTime End) NormalizeRange(DateTime startDateUtc, DateTime endDateUtc)
  {
    // Treat inputs as UTC dates. Clamp to [start-of-day, end-of-day], swap if reversed.
    var s = DateTime.SpecifyKind(startDateUtc.Date, DateTimeKind.Utc);
    var e = DateTime.SpecifyKind(endDateUtc.Date, DateTimeKind.Utc);
    if (e < s) (s, e) = (e, s);
    return (s, e);
  }
}
