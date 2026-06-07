namespace AoDaiNhaUyen.Application.DTOs.Dashboard;

/// <summary>
/// Tổng quan dashboard.
/// </summary>
public sealed record DashboardSummaryDto(
  decimal TotalRevenue,
  int TotalOrders,
  int TotalUsers,
  int TotalProducts,
  double RevenueGrowth,
  double OrdersGrowth,
  double UsersGrowth,
  double ProductsGrowth);

/// <summary>
/// Điểm dữ liệu doanh thu theo ngày.
/// </summary>
public sealed record RevenuePointDto(
  DateTime Date,
  decimal Revenue,
  int Orders);

/// <summary>
/// Danh sách điểm doanh thu.
/// </summary>
public sealed record RevenueDataDto(
  string Period,
  IReadOnlyList<RevenuePointDto> Points);

/// <summary>
/// Thống kê trạng thái đơn hàng.
/// </summary>
public sealed record OrderStatusDistributionDto(
  int Pending,
  int Confirmed,
  int Processing,
  int Shipping,
  int Completed,
  int Cancelled,
  int Returned);

/// <summary>
/// Đơn hàng gần đây.
/// </summary>
public sealed record RecentOrderDto(
  Guid Id,
  string OrderCode,
  string CustomerName,
  decimal TotalAmount,
  string Status,
  DateTimeOffset CreatedAt);

/// <summary>
/// Sản phẩm bán chạy.
/// </summary>
public sealed record TopProductDto(
  Guid? ProductId,
  string ProductName,
  string? ImageUrl,
  int SoldCount,
  decimal Revenue);

/// <summary>
/// Điểm tăng trưởng người dùng.
/// </summary>
public sealed record UserGrowthPointDto(
  DateTime Date,
  int NewUsers,
  int TotalUsers);

/// <summary>
/// Danh sách điểm tăng trưởng người dùng.
/// </summary>
public sealed record UserGrowthDataDto(
  IReadOnlyList<UserGrowthPointDto> Points);
