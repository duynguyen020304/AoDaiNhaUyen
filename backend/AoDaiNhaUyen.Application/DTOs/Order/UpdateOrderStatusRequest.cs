namespace AoDaiNhaUyen.Application.DTOs.Order;

/// <summary>
/// Yêu cầu cập nhật trạng thái đơn hàng.
/// </summary>
public sealed record UpdateOrderStatusRequest(string Status);

/// <summary>
/// Yêu cầu tạo shipment.
/// </summary>
public sealed record CreateShipmentRequest(string? Carrier, string? TrackingNumber);

/// <summary>
/// Yêu cầu cập nhật trạng thái shipment.
/// </summary>
public sealed record UpdateShipmentStatusRequest(string Status);
