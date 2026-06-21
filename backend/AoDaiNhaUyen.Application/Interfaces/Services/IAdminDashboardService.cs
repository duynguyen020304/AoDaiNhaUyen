using AoDaiNhaUyen.Application.DTOs.Dashboard;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminDashboardService
{
  Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);

  Task<RevenueDataDto> GetRevenueAsync(int periodDays, CancellationToken ct = default);

  Task<OrderStatusDistributionDto> GetOrdersByStatusAsync(CancellationToken ct = default);

  Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int limit, CancellationToken ct = default);

  Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int limit, CancellationToken ct = default);

  Task<UserGrowthDataDto> GetUserGrowthAsync(int periodDays, CancellationToken ct = default);

  /// <summary>Revenue series (daily points) for an explicit UTC date range.
  /// End date is inclusive (filtered to end-of-day). Use for "doanh thu từ X đến Y".</summary>
  Task<RevenueDataDto> GetRevenueByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default);

  /// <summary>Order status distribution within an explicit UTC date range.
  /// End date is inclusive (filtered to end-of-day).</summary>
  Task<OrderStatusDistributionDto> GetOrdersByStatusByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default);

  /// <summary>Top products by revenue within an explicit UTC date range.</summary>
  Task<IReadOnlyList<TopProductDto>> GetTopProductsByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken ct = default);

  /// <summary>Aggregated order/revenue metrics within an explicit UTC date range.
  /// Returns total orders, total revenue (paid), cancelled count, AOV.</summary>
  Task<DashboardRangeMetricsDto> GetRangeMetricsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default);
}
