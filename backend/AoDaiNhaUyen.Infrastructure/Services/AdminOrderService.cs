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
      o.Items.Select(i => new AdminOrderItemDetail(
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
}
