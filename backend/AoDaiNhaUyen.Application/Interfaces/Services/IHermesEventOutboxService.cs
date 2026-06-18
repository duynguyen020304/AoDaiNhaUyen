using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesEventOutboxService
{
  Task<PagedResult<HermesEventOutboxListItemResponse>> ListEventsAsync(HermesEventOutboxSearchRequest request, CancellationToken cancellationToken);
  Task<HermesEventOutboxResponse?> GetEventAsync(Guid id, CancellationToken cancellationToken);
  Task<bool> RetryEventAsync(Guid id, CancellationToken cancellationToken);
  Task<bool> CancelEventAsync(Guid id, CancellationToken cancellationToken);
}
