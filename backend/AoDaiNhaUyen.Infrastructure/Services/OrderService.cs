using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.DTOs.Order;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class OrderService(
  AppDbContext dbContext,
  IStockService stockService,
  IHermesEventOutboxPublisher hermesEvents) : IOrderService
{
  private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
  {
    ["pending"] = ["confirmed", "cancelled"],
    ["confirmed"] = ["processing", "cancelled"],
    ["processing"] = ["shipping", "cancelled"],
    ["shipping"] = ["completed", "failed"],
    ["completed"] = ["returned"],
  };

  public async Task<PagedResult<RecentOrderDto>> GetAdminOrdersAsync(
    string? status,
    string? search,
    DateTime? fromDate,
    DateTime? toDate,
    decimal? minTotal,
    decimal? maxTotal,
    string? sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted);

    if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
    {
      query = query.Where(o => o.OrderStatus == status);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = search.Trim().ToLower();
      query = query.Where(o =>
        o.OrderCode.ToLower().Contains(term) ||
        o.RecipientName.ToLower().Contains(term) ||
        o.RecipientPhone.Contains(term) ||
        o.User.FullName.ToLower().Contains(term) ||
        (o.User.Email != null && o.User.Email.ToLower().Contains(term)) ||
        (o.User.Phone != null && o.User.Phone.Contains(term)));
    }

    if (fromDate.HasValue)
    {
      var start = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
      query = query.Where(o => o.CreatedAt >= start);
    }

    if (toDate.HasValue)
    {
      var endExclusive = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
      query = query.Where(o => o.CreatedAt < endExclusive);
    }

    if (minTotal.HasValue)
      query = query.Where(o => o.TotalAmount >= minTotal.Value);

    if (maxTotal.HasValue)
      query = query.Where(o => o.TotalAmount <= maxTotal.Value);

    query = sort switch
    {
      "oldest" => query.OrderBy(o => o.CreatedAt),
      "amount_desc" => query.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.CreatedAt),
      "amount_asc" => query.OrderBy(o => o.TotalAmount).ThenByDescending(o => o.CreatedAt),
      _ => query.OrderByDescending(o => o.CreatedAt),
    };

    var total = await query.CountAsync(cancellationToken);
    var items = await query
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(o => new RecentOrderDto(
        o.Id,
        o.OrderCode,
        o.User.FullName,
        o.TotalAmount,
        o.OrderStatus,
        new DateTimeOffset(o.CreatedAt, TimeSpan.Zero),
        o.CompletedAt.HasValue ? new DateTimeOffset(o.CompletedAt.Value, TimeSpan.Zero) : null))
      .ToListAsync(cancellationToken);

    return new PagedResult<RecentOrderDto>(items, total, page, pageSize);
  }

  public async Task<OrderUpdateResult> UpdateStatusAsync(Guid orderId, string newStatus, CancellationToken cancellationToken = default)
  {
    var order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
    if (order is null)
      return Fail("order_not_found", "Đơn hàng không tồn tại.", orderId);

    if (!AllowedTransitions.TryGetValue(order.OrderStatus, out var allowed))
      return Fail("invalid_current_status", $"Trạng thái '{order.OrderStatus}' không thể chuyển tiếp.", orderId);

    if (!allowed.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
      return Fail("invalid_transition", $"Không thể chuyển từ '{order.OrderStatus}' sang '{newStatus}'.", orderId);

    var oldStatus = order.OrderStatus;
    var now = DateTime.UtcNow;
    order.OrderStatus = newStatus;
    order.UpdatedAt = now;

    if (newStatus is "confirmed") order.ConfirmedAt ??= now;
    if (newStatus is "completed") order.CompletedAt = now;
    if (newStatus is "returned")
    {
      await hermesEvents.EnqueueAdminOrderEventAsync(
        "cod_rts_alert",
        order.Id,
        new
        {
          orderId = order.Id,
          orderCode = order.OrderCode,
          oldStatus,
          newStatus,
          totalAmount = order.TotalAmount,
          province = order.Province,
          district = order.District,
          phoneLast4 = Last4(order.RecipientPhone),
          riskReason = "order_returned",
          detectedAt = DateTimeOffset.UtcNow
        },
        $"cod_rts_alert:Order:{order.Id:N}:{oldStatus}:returned:{now.Date.Ticks}",
        cancellationToken);
    }
    if (newStatus is "cancelled")
    {
      order.CancelledAt = now;
      await RestoreStockForOrder(order, cancellationToken);
    }

    await hermesEvents.EnqueueAdminOrderEventAsync(
      "order_status_changed",
      order.Id,
      new
      {
        orderId = order.Id,
        orderCode = order.OrderCode,
        oldStatus,
        newStatus,
        totalAmount = order.TotalAmount,
        subtotal = order.Subtotal,
        discountAmount = order.DiscountAmount,
        shippingFee = order.ShippingFee,
        province = order.Province,
        district = order.District,
        changedAt = now
      },
      $"order_status_changed:Order:{order.Id:N}:{oldStatus}:{newStatus}:{now.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);
    return new OrderUpdateResult(true, null, null, orderId, newStatus);
  }

  public async Task<OrderUpdateResult> CreateShipmentAsync(Guid orderId, string? carrier, string? trackingNumber, CancellationToken cancellationToken = default)
  {
    var order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
    if (order is null)
      return Fail("order_not_found", "Đơn hàng không tồn tại.", orderId);

    // Update order status to shipping
    if (!AllowedTransitions.TryGetValue(order.OrderStatus, out var allowed) || !allowed.Contains("shipping", StringComparer.OrdinalIgnoreCase))
      return Fail("invalid_transition", $"Không thể tạo shipment khi đơn hàng ở trạng thái '{order.OrderStatus}'.", orderId);

    var now = DateTime.UtcNow;

    var shipment = new Shipment
    {
      OrderId = orderId,
      Carrier = carrier,
      TrackingNumber = trackingNumber,
      ShippingStatus = "shipped",
      ShippedAt = now,
      CreatedAt = now
    };
    dbContext.Shipments.Add(shipment);

    order.OrderStatus = "shipping";
    order.UpdatedAt = now;

    await hermesEvents.EnqueueAdminOrderEventAsync(
      "shipment_created",
      order.Id,
      new { orderId = order.Id, orderCode = order.OrderCode, shipmentId = shipment.Id, carrier, trackingNumberPresent = !string.IsNullOrWhiteSpace(trackingNumber), shippingStatus = shipment.ShippingStatus },
      $"shipment_created:Order:{order.Id:N}:{shipment.Id:N}:{now.Ticks}",
      cancellationToken);

    await hermesEvents.EnqueueAdminOrderEventAsync(
      "order_status_changed",
      order.Id,
      new { orderId = order.Id, orderCode = order.OrderCode, oldStatus = "processing", newStatus = "shipping", totalAmount = order.TotalAmount },
      $"order_status_changed:Order:{order.Id:N}:processing:shipping:{now.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);
    return new OrderUpdateResult(true, null, null, orderId, "shipping");
  }

  public async Task<OrderUpdateResult> UpdateShipmentStatusAsync(Guid shipmentId, string newStatus, CancellationToken cancellationToken = default)
  {
    var shipment = await dbContext.Shipments.FindAsync([shipmentId], cancellationToken);
    if (shipment is null)
      return Fail("shipment_not_found", "Shipment không tồn tại.", Guid.Empty);

    var validShippingStatuses = new[] { "pending", "packed", "shipped", "delivered", "failed", "returned" };
    if (!validShippingStatuses.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
      return Fail("invalid_status", $"Trạng thái shipment '{newStatus}' không hợp lệ.", shipment.OrderId);

    var oldShippingStatus = shipment.ShippingStatus;
    shipment.ShippingStatus = newStatus;

    if (newStatus is "delivered")
    {
      shipment.DeliveredAt = DateTime.UtcNow;
      var order = await dbContext.Orders.FindAsync([shipment.OrderId], cancellationToken);
      if (order is not null && order.OrderStatus == "shipping")
      {
        order.OrderStatus = "completed";
        order.CompletedAt = DateTime.UtcNow;
      }
    }

    await hermesEvents.EnqueueAdminOrderEventAsync(
      "shipment_status_changed",
      shipment.OrderId,
      new { orderId = shipment.OrderId, shipmentId = shipment.Id, oldStatus = oldShippingStatus, newStatus, deliveredAt = shipment.DeliveredAt },
      $"shipment_status_changed:Order:{shipment.OrderId:N}:{shipment.Id:N}:{oldShippingStatus}:{newStatus}:{DateTime.UtcNow.Ticks}",
      cancellationToken);

    if (string.Equals(newStatus, "failed", StringComparison.OrdinalIgnoreCase)
      || string.Equals(newStatus, "returned", StringComparison.OrdinalIgnoreCase))
    {
      var order = await dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shipment.OrderId, cancellationToken);
      await hermesEvents.EnqueueAdminOrderEventAsync(
        "cod_rts_alert",
        shipment.OrderId,
        new
        {
          orderId = shipment.OrderId,
          orderCode = order?.OrderCode,
          shipmentId = shipment.Id,
          shipment.Carrier,
          trackingNumberPresent = !string.IsNullOrWhiteSpace(shipment.TrackingNumber),
          oldStatus = oldShippingStatus,
          newStatus,
          province = order?.Province,
          district = order?.District,
          phoneLast4 = Last4(order?.RecipientPhone),
          riskReason = string.Equals(newStatus, "returned", StringComparison.OrdinalIgnoreCase) ? "shipment_returned" : "delivery_failed",
          detectedAt = DateTimeOffset.UtcNow
        },
        $"cod_rts_alert:Order:{shipment.OrderId:N}:{shipment.Id:N}:{newStatus}:{DateTime.UtcNow.Date.Ticks}",
        cancellationToken);
    }

    if (string.Equals(newStatus, "failed", StringComparison.OrdinalIgnoreCase))
    {
      var order = await dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shipment.OrderId, cancellationToken);
      await hermesEvents.EnqueueAdminOrderEventAsync(
        "delivery_failed_alert",
        shipment.OrderId,
        new
        {
          orderId = shipment.OrderId,
          orderCode = order?.OrderCode,
          shipmentId = shipment.Id,
          shipment.Carrier,
          trackingNumberPresent = !string.IsNullOrWhiteSpace(shipment.TrackingNumber),
          oldStatus = oldShippingStatus,
          newStatus,
          province = order?.Province,
          district = order?.District,
          phoneLast4 = Last4(order?.RecipientPhone),
          detectedAt = DateTimeOffset.UtcNow
        },
        $"delivery_failed_alert:Order:{shipment.OrderId:N}:{shipment.Id:N}:{DateTime.UtcNow.Date.Ticks}",
        cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return new OrderUpdateResult(true, null, null, shipment.OrderId, newStatus);
  }

  public async Task<OrderUpdateResult> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    var order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
    if (order is null)
      return Fail("order_not_found", "Đơn hàng không tồn tại.", orderId);

    var cancellableStatuses = new[] { "pending", "confirmed" };
    if (!cancellableStatuses.Contains(order.OrderStatus, StringComparer.OrdinalIgnoreCase))
      return Fail("cannot_cancel", $"Đơn hàng ở trạng thái '{order.OrderStatus}' không thể hủy.", orderId);

    var oldStatus = order.OrderStatus;
    var now = DateTime.UtcNow;
    order.OrderStatus = "cancelled";
    order.CancelledAt = now;
    order.UpdatedAt = now;

    await RestoreStockForOrder(order, cancellationToken);

    await hermesEvents.EnqueueAdminOrderEventAsync(
      "order_status_changed",
      order.Id,
      new { orderId = order.Id, orderCode = order.OrderCode, oldStatus, newStatus = "cancelled", totalAmount = order.TotalAmount },
      $"order_status_changed:Order:{order.Id:N}:{oldStatus}:cancelled:{now.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);
    return new OrderUpdateResult(true, null, null, orderId, "cancelled");
  }

  private async Task RestoreStockForOrder(Order order, CancellationToken cancellationToken)
  {
    var items = await dbContext.OrderItems
      .Where(oi => oi.OrderId == order.Id && oi.VariantId.HasValue)
      .ToListAsync(cancellationToken);

    foreach (var item in items)
    {
      await stockService.ReleaseStockAsync(item.VariantId!.Value, item.Quantity, cancellationToken);
    }
  }

  private static string Last4(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var digits = new string(value.Where(char.IsDigit).ToArray());
    if (digits.Length <= 4) return digits;
    return digits[^4..];
  }

  private static OrderUpdateResult Fail(string code, string message, Guid orderId)
  {
    return new OrderUpdateResult(false, code, message, orderId, null);
  }
}
