using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesEventProcessor
{
  Task ProcessAsync(HermesEventOutbox item, CancellationToken cancellationToken);
}
