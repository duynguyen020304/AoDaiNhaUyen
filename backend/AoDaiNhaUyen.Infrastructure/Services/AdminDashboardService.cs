using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminDashboardService(AppDbContext dbContext) : IAdminDashboardService
{
  public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
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
    var start = DateTime.UtcNow.Date.AddDays(-period);

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
      .Select(g => new { ProductId = g.Key, ImageUrl = g.First().ImageUrl })
      .ToListAsync(ct);

    var imageLookup = images.ToDictionary(x => x.ProductId, x => x.ImageUrl);

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
    var start = DateTime.UtcNow.Date.AddDays(-period);

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
}
