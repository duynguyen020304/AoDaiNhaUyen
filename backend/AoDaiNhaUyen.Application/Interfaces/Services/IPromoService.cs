using AoDaiNhaUyen.Application.DTOs.Promo;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IPromoService
{
  /// <summary>
  /// Validate mã giảm giá cho đơn hàng. Không lưu vào DB.
  /// </summary>
  Task<PromoValidationResult> ValidateAsync(string code, decimal subtotal, CancellationToken cancellationToken = default);

  /// <summary>
  /// Áp dụng mã giảm giá cho đơn hàng (gọi trong transaction).
  /// </summary>
  Task<PromoValidationResult> ApplyAsync(Guid orderId, string code, decimal subtotal, CancellationToken cancellationToken = default);
}
