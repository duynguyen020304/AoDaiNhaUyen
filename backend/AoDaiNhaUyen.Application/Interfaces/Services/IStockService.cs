using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStockService
{
  /// <summary>
  /// Trừ tồn kho nguyên tử (dùng trong transaction). Trả về false nếu không đủ.
  /// </summary>
  Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Hoàn tồn kho (khi hủy đơn).
  /// </summary>
  Task ReleaseStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Lấy danh sách sản phẩm sắp hết hàng.
  /// </summary>
  Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(int threshold = 5, CancellationToken cancellationToken = default);
}
