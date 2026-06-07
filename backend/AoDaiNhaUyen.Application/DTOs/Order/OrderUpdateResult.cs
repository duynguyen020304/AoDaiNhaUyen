namespace AoDaiNhaUyen.Application.DTOs.Order;

/// <summary>
/// Kết quả cập nhật đơn hàng / shipment.
/// </summary>
public sealed record OrderUpdateResult(
  bool Success,
  string? ErrorCode,
  string? ErrorMessage,
  Guid OrderId,
  string? NewStatus);
