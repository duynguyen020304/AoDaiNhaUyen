using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IPromoCostService
{
  Task CreateOrderSnapshotAsync(Order order, string? promoCode, decimal normalShippingFee, CancellationToken cancellationToken = default);
  Task<PromoPerformanceDto?> GetPromoPerformanceAsync(Guid promoCodeId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
