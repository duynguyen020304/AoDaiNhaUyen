using System.Text;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.DTOs.Checkout;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CheckoutService(
  AppDbContext dbContext,
  ICartRepository cartRepository,
  IEmailService emailService) : ICheckoutService
{
  public async Task<AuthResult<CheckoutResultDto>> CheckoutAsync(long userId, CheckoutRequestDto request, CancellationToken cancellationToken = default)
  {
    if (!string.Equals(request.PaymentMethod, "cash", StringComparison.OrdinalIgnoreCase)
      && !string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase))
    {
      return AuthResult<CheckoutResultDto>.Failure("invalid_payment_method", "Phương thức thanh toán không hợp lệ.");
    }

    var user = await dbContext.Users
      .AsNoTracking()
      .FirstOrDefaultAsync(value => value.Id == userId, cancellationToken);
    if (user is null)
    {
      return AuthResult<CheckoutResultDto>.Failure("user_not_found", "Người dùng không tồn tại.");
    }

    var cart = await cartRepository.GetByUserIdWithItemsAsync(userId, cancellationToken);
    if (cart is null || cart.Items.Count == 0)
    {
      return AuthResult<CheckoutResultDto>.Failure("cart_empty", "Giỏ hàng đang trống.");
    }

    var resolvedAddress = await ResolveAddressAsync(userId, request, cancellationToken);
    if (resolvedAddress is null)
    {
      return AuthResult<CheckoutResultDto>.Failure("invalid_address", "Thông tin giao hàng không hợp lệ.");
    }

    foreach (var item in cart.Items)
    {
      if (item.Variant.Product.Status != "active" || item.Variant.Status != "active")
      {
        return AuthResult<CheckoutResultDto>.Failure("variant_not_available", "Có sản phẩm trong giỏ hàng không còn khả dụng.");
      }

      if (item.Variant.StockQty < item.Quantity)
      {
        return AuthResult<CheckoutResultDto>.Failure("insufficient_stock", "Số lượng tồn kho không đủ cho một hoặc nhiều sản phẩm.");
      }
    }

    var now = DateTime.UtcNow;
    var subtotal = cart.Items.Sum(item => (item.Variant.SalePrice ?? item.Variant.Price) * item.Quantity);
    const decimal shippingFee = 25000m;
    const decimal discountAmount = 0m;
    var totalAmount = subtotal + shippingFee - discountAmount;

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    try
    {
      var order = new Order
      {
        OrderCode = GenerateOrderCode(now),
        UserId = userId,
        AddressId = resolvedAddress.AddressId,
        RecipientName = resolvedAddress.RecipientName,
        RecipientPhone = resolvedAddress.RecipientPhone,
        Province = resolvedAddress.Province,
        District = resolvedAddress.District,
        Ward = resolvedAddress.Ward,
        AddressLine = resolvedAddress.AddressLine,
        Subtotal = subtotal,
        DiscountAmount = discountAmount,
        ShippingFee = shippingFee,
        TotalAmount = totalAmount,
        OrderStatus = "confirmed",
        Note = request.Note,
        PlacedAt = now,
        ConfirmedAt = now,
        CreatedAt = now,
        UpdatedAt = now
      };

      foreach (var item in cart.Items)
      {
        var unitPrice = item.Variant.SalePrice ?? item.Variant.Price;
        order.Items.Add(new OrderItem
        {
          ProductId = item.Variant.ProductId,
          VariantId = item.VariantId,
          ProductName = item.Variant.Product.Name,
          Sku = item.Variant.Sku,
          Size = item.Variant.Size,
          Color = item.Variant.Color,
          UnitPrice = unitPrice,
          Quantity = item.Quantity,
          LineTotal = unitPrice * item.Quantity,
          IsCustomTailoring = false,
          Note = request.Note
        });

        item.Variant.StockQty -= item.Quantity;
      }

      order.Payment = new Payment
      {
        Amount = totalAmount,
        PaidAt = now,
        Note = "paid_successfully",
        CreatedAt = now
      };

      dbContext.Orders.Add(order);
      dbContext.CartItems.RemoveRange(cart.Items);
      cart.UpdatedAt = now;

      await dbContext.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);

      var result = new CheckoutResultDto(
        order.Id,
        order.OrderCode,
        order.OrderStatus,
        "paid",
        order.Subtotal,
        order.DiscountAmount,
        order.ShippingFee,
        order.TotalAmount,
        order.PlacedAt);

      if (!string.IsNullOrWhiteSpace(user.Email))
      {
        await emailService.SendEmailAsync(
          user.Email,
          $"Hóa đơn đơn hàng {order.OrderCode}",
          BuildInvoiceEmail(order, "paid"),
          cancellationToken);
      }

      return AuthResult<CheckoutResultDto>.Success(result);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync(cancellationToken);
      return AuthResult<CheckoutResultDto>.Failure("checkout_failed", $"Thanh toán thất bại: {ex.Message}");
    }
  }

  private static string GenerateOrderCode(DateTime now)
  {
    return $"AD-{now:yyyyMMddHHmmss}";
  }

  private async Task<ResolvedAddress?> ResolveAddressAsync(long userId, CheckoutRequestDto request, CancellationToken cancellationToken)
  {
    if (request.AddressId.HasValue)
    {
      var address = await dbContext.UserAddresses
        .AsNoTracking()
        .FirstOrDefaultAsync(value => value.Id == request.AddressId.Value && value.UserId == userId, cancellationToken);

      if (address is null)
      {
        return null;
      }

      return new ResolvedAddress(
        address.Id,
        address.RecipientName,
        address.RecipientPhone,
        address.Province,
        address.District,
        address.Ward,
        address.AddressLine);
    }

    if (request.Address is null
      || string.IsNullOrWhiteSpace(request.Address.RecipientName)
      || string.IsNullOrWhiteSpace(request.Address.RecipientPhone)
      || string.IsNullOrWhiteSpace(request.Address.Province)
      || string.IsNullOrWhiteSpace(request.Address.District)
      || string.IsNullOrWhiteSpace(request.Address.AddressLine))
    {
      return null;
    }

    return new ResolvedAddress(
      null,
      request.Address.RecipientName.Trim(),
      request.Address.RecipientPhone.Trim(),
      request.Address.Province.Trim(),
      request.Address.District.Trim(),
      request.Address.Ward?.Trim(),
      request.Address.AddressLine.Trim());
  }

  private static string BuildInvoiceEmail(Order order, string paymentStatus)
  {
    var paymentStatusLabel = paymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase)
      ? "Đã thanh toán"
      : paymentStatus;

    var wardPart = string.IsNullOrWhiteSpace(order.Ward) ? string.Empty : $", {order.Ward}";
    var builder = new StringBuilder();
    builder.Append("<h1>Hóa đơn đặt hàng</h1>");
    builder.Append($"<p>Mã đơn hàng: <strong>{order.OrderCode}</strong></p>");
    builder.Append($"<p>Trạng thái thanh toán: <strong>{paymentStatusLabel}</strong></p>");
    builder.Append($"<p>Người nhận: {order.RecipientName} - {order.RecipientPhone}</p>");
    builder.Append($"<p>Địa chỉ: {order.AddressLine}{wardPart}, {order.District}, {order.Province}</p>");
    builder.Append("<table cellpadding=\"8\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse; width:100%\">");
    builder.Append("<thead><tr><th>Sản phẩm</th><th>Phân loại</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>");

    foreach (var item in order.Items)
    {
      var variantLabel = string.Join(" / ", new[] { item.Size, item.Color }
        .Where(value => !string.IsNullOrWhiteSpace(value)));

      builder.Append("<tr>");
      builder.Append($"<td>{item.ProductName}</td>");
      builder.Append($"<td>{(string.IsNullOrWhiteSpace(variantLabel) ? "-" : variantLabel)}</td>");
      builder.Append($"<td>{item.Quantity}</td>");
      builder.Append($"<td>{item.UnitPrice:N0} VND</td>");
      builder.Append($"<td>{item.LineTotal:N0} VND</td>");
      builder.Append("</tr>");
    }

    builder.Append("</tbody></table>");
    builder.Append($"<p>Tạm tính: <strong>{order.Subtotal:N0} VND</strong></p>");
    builder.Append($"<p>Phí vận chuyển: <strong>{order.ShippingFee:N0} VND</strong></p>");
    builder.Append($"<p>Tổng thanh toán: <strong>{order.TotalAmount:N0} VND</strong></p>");
    return builder.ToString();
  }

  private sealed record ResolvedAddress(
    long? AddressId,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string District,
    string? Ward,
    string AddressLine);
}
