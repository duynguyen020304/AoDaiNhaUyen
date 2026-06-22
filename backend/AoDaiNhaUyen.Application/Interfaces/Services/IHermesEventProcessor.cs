using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesEventProcessor
{
  Task ProcessAsync(HermesEventOutbox item, CancellationToken cancellationToken);

  /// <summary>
  /// Process multiple events in a single Hermes API call, producing ONE
  /// comprehensive report covering all of them. Returns the IDs of the events
  /// that were successfully processed (and whose status the caller may mark
  /// <c>completed</c>). Any event NOT in the returned set must be retried by the
  /// caller via per-event <see cref="ProcessAsync"/> (the batch made no durable
  /// change for those). Implementations must not throw for a normal batch
  /// failure — they return an empty/partial set so the caller can fall back.
  /// </summary>
  Task<IReadOnlyList<Guid>> ProcessBatchAsync(IReadOnlyList<HermesEventOutbox> items, CancellationToken cancellationToken);
}
