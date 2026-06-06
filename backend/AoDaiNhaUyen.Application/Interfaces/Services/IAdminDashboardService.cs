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
}
