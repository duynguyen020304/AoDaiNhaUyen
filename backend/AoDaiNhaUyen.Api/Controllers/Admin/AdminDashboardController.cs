using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminDashboardController(IAdminDashboardService dashboardService) : ControllerBase
{
  /// <summary>
  /// Lấy tổng quan dashboard (doanh thu, đơn hàng, người dùng, sản phẩm).
  /// </summary>
  [HttpGet("summary")]
  public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
  {
    var summary = await dashboardService.GetSummaryAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(summary));
  }

  /// <summary>
  /// Lấy dữ liệu doanh thu theo thời gian.
  /// </summary>
  [HttpGet("revenue")]
  public async Task<IActionResult> GetRevenue(
    [FromQuery] int period = 30,
    CancellationToken cancellationToken = default)
  {
    var data = await dashboardService.GetRevenueAsync(period, cancellationToken);
    return Ok(ApiResponseFactory.Success(data));
  }

  /// <summary>
  /// Lấy thống kê trạng thái đơn hàng.
  /// </summary>
  [HttpGet("orders-by-status")]
  public async Task<IActionResult> GetOrdersByStatus(CancellationToken cancellationToken = default)
  {
    var distribution = await dashboardService.GetOrdersByStatusAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(distribution));
  }

  /// <summary>
  /// Lấy danh sách đơn hàng gần đây.
  /// </summary>
  [HttpGet("recent-orders")]
  public async Task<IActionResult> GetRecentOrders(
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default)
  {
    var orders = await dashboardService.GetRecentOrdersAsync(limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(orders));
  }

  /// <summary>
  /// Lấy danh sách sản phẩm bán chạy.
  /// </summary>
  [HttpGet("top-products")]
  public async Task<IActionResult> GetTopProducts(
    [FromQuery] int limit = 5,
    CancellationToken cancellationToken = default)
  {
    var products = await dashboardService.GetTopProductsAsync(limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(products));
  }

  /// <summary>
  /// Lấy dữ liệu tăng trưởng người dùng.
  /// </summary>
  [HttpGet("user-growth")]
  public async Task<IActionResult> GetUserGrowth(
    [FromQuery] int period = 30,
    CancellationToken cancellationToken = default)
  {
    var data = await dashboardService.GetUserGrowthAsync(period, cancellationToken);
    return Ok(ApiResponseFactory.Success(data));
  }
}
