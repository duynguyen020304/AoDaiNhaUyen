using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IOrderAttributionService
{
  Task CreateAsync(
    Order order,
    string? anonymousSessionId,
    string? promoCode,
    decimal normalShippingFee,
    CancellationToken cancellationToken = default);
}
