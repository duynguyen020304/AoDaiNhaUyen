using System.ComponentModel;
using System.Text.Json;
using AoDaiNhaUyen.Application.Interfaces.Services;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminDashboardTools
{
  [McpServerTool, Description("Lấy tổng quan dashboard: tổng doanh thu, đơn hàng, người dùng, sản phẩm.")]
  public static async Task<string> GetDashboardSummary(
    [Description("Khoảng thời gian: today, week, month. Mặc định: week")] string period = "week",
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return Error("DashboardService", "Dashboard service chưa được inject.");
    var s = await dashboard.GetSummaryAsync(CancellationToken.None);
    return JsonSerializer.Serialize(s);
  }

  [McpServerTool, Description("Lấy dữ liệu doanh thu theo khoảng thời gian.")]
  public static async Task<string> GetRevenue(
    [Description("Số ngày: 7, 30, hoặc 90. Mặc định: 7")] int period = 7,
    IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return Error("DashboardService", "Dashboard service chưa được inject.");
    var r = await dashboard.GetRevenueAsync(period, CancellationToken.None);
    return JsonSerializer.Serialize(r);
  }

  [McpServerTool, Description("Lấy phân phối đơn hàng theo trạng thái.")]
  public static async Task<string> GetOrdersByStatus(IAdminDashboardService? dashboard = null)
  {
    if (dashboard is null) return Error("DashboardService", "Dashboard service chưa được inject.");
    var o = await dashboard.GetOrdersByStatusAsync(CancellationToken.None);
    return JsonSerializer.Serialize(o);
  }

  private static string Error(string code, string msg) =>
    $"{{\"error\": {{\"code\": \"{code}\", \"message\": \"{msg}\"}}}}";
}
