using AoDaiNhaUyen.Application.DTOs.Order;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminOrderService(
  AppDbContext dbContext,
  IOrderService orderService) : IAdminOrderService
{
  public async Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(
    string? status, int limit, CancellationToken ct = default)
  {
    var query = dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted);

    if (!string.IsNullOrWhiteSpace(status))
      query = query.Where(o => o.OrderStatus == status);

    var orders = await query
      .OrderByDescending(o => o.CreatedAt)
      .Take(Math.Clamp(limit, 1, 50))
      .Select(o => new AdminOrderListItem(
        o.Id,
        o.OrderCode,
        o.User.FullName,
        o.TotalAmount,
        o.OrderStatus,
        o.Items.Count,
        o.CreatedAt))
      .ToListAsync(ct);

    return orders;
  }

  public async Task<IReadOnlyList<AdminOrderListItem>> GetOrdersByRangeAsync(
    string? status, DateTime? startDateUtc, DateTime? endDateUtc, int limit, CancellationToken ct = default)
  {
    var query = dbContext.Orders
      .AsNoTracking()
      .Where(o => !o.IsDeleted);

    if (!string.IsNullOrWhiteSpace(status))
      query = query.Where(o => o.OrderStatus == status);

    // Normalize: start = start-of-day UTC, endExclusive = day after end UTC.
    if (startDateUtc.HasValue)
    {
      var s = DateTime.SpecifyKind(startDateUtc.Value.Date, DateTimeKind.Utc);
      query = query.Where(o => o.CreatedAt >= s);
    }
    if (endDateUtc.HasValue)
    {
      var endExclusive = DateTime.SpecifyKind(endDateUtc.Value.Date, DateTimeKind.Utc).AddDays(1);
      query = query.Where(o => o.CreatedAt < endExclusive);
    }

    var orders = await query
      .OrderByDescending(o => o.CreatedAt)
      .Take(Math.Clamp(limit, 1, 100))
      .Select(o => new AdminOrderListItem(
        o.Id,
        o.OrderCode,
        o.User.FullName,
        o.TotalAmount,
        o.OrderStatus,
        o.Items.Count,
        o.CreatedAt))
      .ToListAsync(ct);

    return orders;
  }

  public Task<AdminOrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default) =>
    ProjectOrderDetails(dbContext.Orders
      .AsNoTracking()
      .Where(o => o.Id == orderId && !o.IsDeleted))
    .FirstOrDefaultAsync(ct);

  public Task<AdminOrderDetail?> GetOrderByCodeAsync(string orderCode, CancellationToken ct = default)
  {
    var normalizedCode = orderCode.Trim();
    return ProjectOrderDetails(dbContext.Orders
        .AsNoTracking()
        .Where(o => o.OrderCode == normalizedCode && !o.IsDeleted))
      .FirstOrDefaultAsync(ct);
  }

  private static IQueryable<AdminOrderDetail> ProjectOrderDetails(IQueryable<Domain.Entities.Order> query) =>
    query.Select(o => new AdminOrderDetail(
      o.Id,
      o.OrderCode,
      o.User.FullName,
      o.User.Email,
      o.Province,
      o.District,
      o.Ward,
      o.AddressLine,
      o.Subtotal,
      o.DiscountAmount,
      o.ShippingFee,
      o.TotalAmount,
      o.OrderStatus,
      o.Note,
      o.CreatedAt,
      o.Items.Where(i => !i.IsDeleted).Select(i => new AdminOrderItemDetail(
        i.Id,
        i.ProductId,
        i.VariantId,
        i.ProductName,
        i.Sku,
        i.Size,
        i.Color,
        i.UnitPrice,
        i.Quantity,
        i.LineTotal)).ToList()));

  public Task<OrderUpdateResult> UpdateStatusAsync(
    Guid orderId, string newStatus, CancellationToken ct = default)
    => orderService.UpdateStatusAsync(orderId, newStatus, ct);

  public Task<OrderUpdateResult> CreateShipmentAsync(
    Guid orderId, string? carrier, string? trackingNumber, CancellationToken ct = default)
    => orderService.CreateShipmentAsync(orderId, carrier, trackingNumber, ct);

  public Task<OrderUpdateResult> CancelOrderAsync(
    Guid orderId, CancellationToken ct = default)
    => orderService.CancelOrderAsync(orderId, ct);

  public async Task<OrderUpdateResult> UpdateOrderAddressAsync(Guid orderId, AdminOrderAddressUpdate request, CancellationToken ct = default)
  {
    var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, ct);
    if (order is null) return Fail(orderId, "not_found", "Không tìm thấy đơn hàng.");
    if (!CanEditOrder(order.OrderStatus)) return Fail(orderId, "invalid_status", "Chỉ được sửa đơn ở trạng thái pending/confirmed/processing.");

    order.RecipientName = request.RecipientName.Trim();
    order.RecipientPhone = request.RecipientPhone.Trim();
    order.Province = request.Province.Trim();
    order.District = request.District.Trim();
    order.Ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
    order.AddressLine = request.AddressLine.Trim();
    order.Note = string.IsNullOrWhiteSpace(request.Note) ? order.Note : request.Note.Trim();
    order.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return Ok(order);
  }

  public async Task<OrderUpdateResult> UpdateOrderItemsAsync(Guid orderId, IReadOnlyList<AdminOrderItemUpdate> items, CancellationToken ct = default)
  {
    if (items.Count == 0) return Fail(orderId, "validation_error", "Cần ít nhất một dòng hàng để cập nhật.");

    var order = await dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, ct);
    if (order is null) return Fail(orderId, "not_found", "Không tìm thấy đơn hàng.");
    if (!CanEditOrder(order.OrderStatus)) return Fail(orderId, "invalid_status", "Chỉ được sửa đơn ở trạng thái pending/confirmed/processing.");

    var itemById = order.Items.Where(i => !i.IsDeleted).ToDictionary(i => i.Id);
    foreach (var update in items)
    {
      if (!itemById.TryGetValue(update.ItemId, out var item)) return Fail(orderId, "item_not_found", $"Không tìm thấy dòng hàng {update.ItemId}.");
      if (update.Quantity <= 0) return Fail(orderId, "validation_error", "Quantity phải lớn hơn 0.");
      item.Quantity = update.Quantity;
      if (update.UnitPrice.HasValue) item.UnitPrice = Math.Max(0m, update.UnitPrice.Value);
      item.LineTotal = item.UnitPrice * item.Quantity;
      item.UpdatedAt = DateTime.UtcNow;
    }

    order.Subtotal = order.Items.Where(i => !i.IsDeleted).Sum(i => i.LineTotal);
    order.TotalAmount = Math.Max(0m, order.Subtotal - order.DiscountAmount + order.ShippingFee);
    order.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return Ok(order);
  }

  public async Task<OrderUpdateResult> DeleteOrderAsync(Guid orderId, CancellationToken ct = default)
  {
    var order = await dbContext.Orders.Include(o => o.Shipments).IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orderId, ct);
    if (order is null || order.IsDeleted) return Fail(orderId, "not_found", "Không tìm thấy đơn hàng hoặc đơn đã bị xóa.");
    if (order.Shipments.Any(s => s.ShippingStatus == "delivered")) return Fail(orderId, "invalid_status", "Không thể xóa đơn đã giao thành công.");

    order.IsDeleted = true;
    order.DeletedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return Ok(order);
  }

  public async Task<OrderUpdateResult> RestoreOrderAsync(Guid orderId, CancellationToken ct = default)
  {
    var order = await dbContext.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orderId, ct);
    if (order is null || !order.IsDeleted) return Fail(orderId, "not_found", "Không tìm thấy đơn hàng đã xóa.");

    order.IsDeleted = false;
    order.DeletedAt = null;
    order.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return Ok(order);
  }

  private static bool CanEditOrder(string status) => status is "pending" or "confirmed" or "processing";
  private static OrderUpdateResult Ok(Domain.Entities.Order order) => new(true, null, null, order.Id, order.OrderStatus);
  private static OrderUpdateResult Fail(Guid orderId, string code, string message) => new(false, code, message, orderId, null);
}
