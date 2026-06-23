using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.DTOs.Order;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IOrderService
{
  /// <summary>
  /// Lấy danh sách đơn hàng cho admin với phân trang và lọc trạng thái.
  /// </summary>
  Task<PagedResult<RecentOrderDto>> GetAdminOrdersAsync(
    string? status,
    string? search,
    DateTime? fromDate,
    DateTime? toDate,
    decimal? minTotal,
    decimal? maxTotal,
    string? sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Cập nhật trạng thái đơn hàng (với state machine validation).
  /// </summary>
  Task<OrderUpdateResult> UpdateStatusAsync(Guid orderId, string newStatus, CancellationToken cancellationToken = default);

  /// <summary>
  /// Tạo shipment cho đơn hàng.
  /// </summary>
  Task<OrderUpdateResult> CreateShipmentAsync(Guid orderId, string? carrier, string? trackingNumber, CancellationToken cancellationToken = default);

  /// <summary>
  /// Cập nhật trạng thái shipment.
  /// </summary>
  Task<OrderUpdateResult> UpdateShipmentStatusAsync(Guid shipmentId, string newStatus, CancellationToken cancellationToken = default);

  /// <summary>
  /// Hủy đơn hàng (customer hoặc admin).
  /// </summary>
  Task<OrderUpdateResult> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
