using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
  /// Xuất báo cáo dashboard theo khoảng ngày dưới dạng PDF.
  /// </summary>
  [HttpGet("report.pdf")]
  public async Task<IActionResult> DownloadReportPdf(
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    CancellationToken cancellationToken = default)
  {
    var end = (toDate ?? DateTime.UtcNow).Date;
    var start = (fromDate ?? end.AddDays(-6)).Date;
    if (end < start) (start, end) = (end, start);

    var metrics = await dashboardService.GetRangeMetricsAsync(start, end, cancellationToken);
    var revenue = await dashboardService.GetRevenueByRangeAsync(start, end, cancellationToken);
    var ordersByStatus = await dashboardService.GetOrdersByStatusByRangeAsync(start, end, cancellationToken);
    var topProducts = await dashboardService.GetTopProductsByRangeAsync(start, end, 10, cancellationToken);

    var pdf = CreateDashboardReportPdf(start, end, metrics, revenue, ordersByStatus, topProducts).GeneratePdf();
    return File(pdf, "application/pdf", $"bao-cao-dashboard-{start:yyyyMMdd}-{end:yyyyMMdd}.pdf");
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

  private static IDocument CreateDashboardReportPdf(
    DateTime start,
    DateTime end,
    DashboardRangeMetricsDto metrics,
    RevenueDataDto revenue,
    OrderStatusDistributionDto ordersByStatus,
    IReadOnlyList<TopProductDto> topProducts)
  {
    var statusRows = new[]
    {
      ("Chờ xác nhận", ordersByStatus.Pending),
      ("Đã xác nhận", ordersByStatus.Confirmed),
      ("Đang xử lý", ordersByStatus.Processing),
      ("Đang giao", ordersByStatus.Shipping),
      ("Hoàn thành", ordersByStatus.Completed),
      ("Đã hủy", ordersByStatus.Cancelled),
      ("Trả hàng", ordersByStatus.Returned),
    }.Where(x => x.Item2 > 0).ToArray();

    return Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(text => text.FontSize(10).FontColor(Colors.Grey.Darken3));

        page.Header().Column(column =>
        {
          column.Spacing(4);
          column.Item().Text("Áo Dài Nhà Uyên · Báo cáo dashboard").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken1);
          column.Item().Text($"Tổng quan kinh doanh {start:dd/MM/yyyy} - {end:dd/MM/yyyy}").FontSize(20).Bold().FontColor(Colors.Grey.Darken4);
          column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });

        page.Content().PaddingVertical(14).Column(column =>
        {
          column.Spacing(14);
          column.Item().Row(row =>
          {
            row.RelativeItem().Element(c => MetricBox(c, "Doanh thu đã thanh toán", Money(metrics.PaidRevenue)));
            row.RelativeItem().Element(c => MetricBox(c, "Đơn hàng", metrics.TotalOrders.ToString("N0")));
            row.RelativeItem().Element(c => MetricBox(c, "AOV", Money(metrics.AverageOrderValue)));
          });

          column.Item().Text("Doanh thu theo ngày").FontSize(13).Bold().FontColor(Colors.Grey.Darken4);
          column.Item().Table(table =>
          {
            table.ColumnsDefinition(columns =>
            {
              columns.RelativeColumn();
              columns.RelativeColumn();
              columns.RelativeColumn();
            });
            HeaderCell(table, "Ngày");
            HeaderCell(table, "Doanh thu");
            HeaderCell(table, "Đơn hàng");
            foreach (var point in revenue.Points)
            {
              BodyCell(table, point.Date.ToString("dd/MM/yyyy"));
              BodyCell(table, Money(point.Revenue));
              BodyCell(table, point.Orders.ToString("N0"));
            }
          });

          column.Item().Row(row =>
          {
            row.RelativeItem().Column(col =>
            {
              col.Item().Text("Trạng thái đơn hàng").FontSize(13).Bold().FontColor(Colors.Grey.Darken4);
              col.Item().PaddingTop(6).Table(table =>
              {
                table.ColumnsDefinition(columns =>
                {
                  columns.RelativeColumn();
                  columns.ConstantColumn(55);
                });
                foreach (var (label, count) in statusRows.DefaultIfEmpty(("Chưa có dữ liệu", 0)))
                {
                  BodyCell(table, label);
                  BodyCell(table, count.ToString("N0"));
                }
              });
            });
            row.RelativeItem().Column(col =>
            {
              col.Item().Text("Top sản phẩm").FontSize(13).Bold().FontColor(Colors.Grey.Darken4);
              col.Item().PaddingTop(6).Table(table =>
              {
                table.ColumnsDefinition(columns =>
                {
                  columns.RelativeColumn();
                  columns.ConstantColumn(45);
                  columns.ConstantColumn(75);
                });
                foreach (var product in topProducts.DefaultIfEmpty(new TopProductDto(null, "Chưa có dữ liệu", null, 0, 0)))
                {
                  BodyCell(table, product.ProductName);
                  BodyCell(table, product.SoldCount.ToString("N0"));
                  BodyCell(table, Money(product.Revenue));
                }
              });
            });
          });
        });

        page.Footer().AlignCenter().Text(text =>
        {
          text.Span("Trang ");
          text.CurrentPageNumber();
          text.Span(" / ");
          text.TotalPages();
        });
      });
    });
  }

  private static void MetricBox(IContainer container, string label, string value)
  {
    container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(column =>
    {
      column.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
      column.Item().Text(value).FontSize(14).Bold().FontColor(Colors.Grey.Darken4);
    });
  }

  private static void HeaderCell(TableDescriptor table, string text)
  {
    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(text).SemiBold();
  }

  private static void BodyCell(TableDescriptor table, string text)
  {
    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(text);
  }

  private static string Money(decimal value) => $"{value:N0} ₫";
}
