using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.DTOs.Order;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin order management service for AI agent operations.</summary>
public interface IAdminOrderService
{
  /// <summary>List orders filtered by status with pagination.</summary>
  Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(
    string? status, int limit, CancellationToken ct = default);

  /// <summary>Get order detail by ID.</summary>
  Task<AdminOrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);

  /// <summary>Get order detail by public order code.</summary>
  Task<AdminOrderDetail?> GetOrderByCodeAsync(string orderCode, CancellationToken ct = default);

  /// <summary>Update order status (delegates to IOrderService).</summary>
  Task<OrderUpdateResult> UpdateStatusAsync(Guid orderId, string newStatus, CancellationToken ct = default);

  /// <summary>Create shipment and transition to shipping.</summary>
  Task<OrderUpdateResult> CreateShipmentAsync(Guid orderId, string? carrier, string? trackingNumber, CancellationToken ct = default);

  /// <summary>Cancel order and restore stock.</summary>
  Task<OrderUpdateResult> CancelOrderAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record AdminOrderListItem(
  Guid Id,
  string OrderCode,
  string? CustomerName,
  decimal TotalAmount,
  string OrderStatus,
  int ItemCount,
  DateTimeOffset CreatedAt);

public sealed record AdminOrderDetail(
  Guid Id,
  string OrderCode,
  string? CustomerName,
  string? CustomerEmail,
  string? Province,
  string? District,
  string? Ward,
  string? AddressLine,
  decimal Subtotal,
  decimal DiscountAmount,
  decimal ShippingFee,
  decimal TotalAmount,
  string OrderStatus,
  string? Note,
  DateTimeOffset CreatedAt,
  IReadOnlyList<AdminOrderItemDetail> Items);

public sealed record AdminOrderItemDetail(
  string ProductName,
  string? Sku,
  string? Size,
  string? Color,
  decimal UnitPrice,
  int Quantity,
  decimal LineTotal);
