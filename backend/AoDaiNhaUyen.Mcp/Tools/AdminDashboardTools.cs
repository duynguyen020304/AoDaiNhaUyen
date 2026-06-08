using Microsoft.AspNetCore.Authorization;
using AoDaiNhaUyen.Mcp.Auth;
using System.ComponentModel;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminDashboardTools
{
  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy tổng quan dashboard: tổng doanh thu, đơn hàng, người dùng, sản phẩm.")]
  public static async Task<string> GetDashboardSummary(
    [Description("Khoảng thời gian: today, week, month. Mặc định: week")] string period = "week",
    CancellationToken cancellationToken = default,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    var s = await dashboard.GetSummaryAsync(cancellationToken);
    return ToolJson.Ok(s);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy dữ liệu doanh thu theo khoảng thời gian.")]
  public static async Task<string> GetRevenue(
    [Description("Số ngày: 7, 30, hoặc 90. Mặc định: 7")] int period = 7,
    CancellationToken cancellationToken = default,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    period = ToolValidation.RevenuePeriod(period);
    var r = await dashboard.GetRevenueAsync(period, cancellationToken);
    return ToolJson.Ok(r);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy phân phối đơn hàng theo trạng thái.")]
  public static async Task<string> GetOrdersByStatus(CancellationToken cancellationToken = default, IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    var o = await dashboard.GetOrdersByStatusAsync(cancellationToken);
    return ToolJson.Ok(o);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy danh sách đơn hàng gần đây theo limit, không phải toàn bộ lịch sử đơn. Dùng để xem nhanh hoạt động mới nhất; nếu cần lọc/truy vết sâu hãy dùng công cụ đơn hàng chuyên biệt.")]
  public static async Task<string> GetRecentOrders(
    [Description("Số lượng đơn hàng. Mặc định: 10")] int limit = 10,
    CancellationToken cancellationToken = default,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    limit = ToolValidation.Limit(limit);
    var o = await dashboard.GetRecentOrdersAsync(limit, cancellationToken);
    return ToolJson.OkWithMeta(
      o,
      new AdminToolResultMeta(
        Page: 1,
        PageSize: limit,
        Total: o.Count,
        TotalPages: o.Count == 0 ? 0 : 1,
        HasMore: o.Count >= limit,
        Completeness: o.Count >= limit ? "partial_page" : "complete_page",
        FiltersApplied: new { limit }),
      message: "Danh sách này bị giới hạn theo limit; không đại diện toàn bộ lịch sử đơn.");
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy top sản phẩm bán chạy theo doanh số với limit. Không dùng để kiểm tra sản phẩm có tồn tại trong catalog; nếu admin hỏi sản phẩm cụ thể hãy dùng list_products/search.")]
  public static async Task<string> GetTopProducts(
    [Description("Số lượng sản phẩm. Mặc định: 5")] int limit = 5,
    CancellationToken cancellationToken = default,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    limit = ToolValidation.Limit(limit);
    var p = await dashboard.GetTopProductsAsync(limit, cancellationToken);
    return ToolJson.OkWithMeta(
      p,
      new AdminToolResultMeta(
        Page: 1,
        PageSize: limit,
        Total: p.Count,
        TotalPages: p.Count == 0 ? 0 : 1,
        HasMore: p.Count >= limit,
        Completeness: p.Count >= limit ? "partial_page" : "complete_page",
        FiltersApplied: new { limit }),
      message: "Danh sách top sản phẩm bị giới hạn theo limit; không dùng để kết luận catalog.");
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy dữ liệu tăng trưởng người dùng.")]
  public static async Task<string> GetUserGrowth(
    [Description("Số ngày phân tích. Mặc định: 30")] int periodDays = 30,
    CancellationToken cancellationToken = default,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return ToolJson.ServiceMissing("DashboardService");
    periodDays = ToolValidation.PeriodDays(periodDays);
    var g = await dashboard.GetUserGrowthAsync(periodDays, cancellationToken);
    return ToolJson.Ok(g);
  }

}
