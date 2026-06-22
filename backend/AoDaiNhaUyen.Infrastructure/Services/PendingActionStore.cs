using System.Collections.Concurrent;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class PendingActionStore : IPendingActionStore
{
  private const int MaxPendingActions = 500;
  private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
  private readonly ConcurrentDictionary<string, AdminPendingAction> _pending = new();

  private readonly ConcurrentDictionary<string, DateTime> _consumed = new();

  public void Add(string actionId, AdminPendingAction pending)
  {
    PruneExpired();
    _pending[actionId] = pending;
    EnforceMaxPendingActions();
  }

  public AdminPendingAction? Remove(string actionId)
  {
    PruneExpired();
    return _pending.TryRemove(actionId, out var p) ? p : null;
  }

  public bool TryMarkConsumed(string actionId)
  {
    PruneExpired();
    return _consumed.TryAdd(actionId, DateTime.UtcNow);
  }

  private void PruneExpired()
  {
    var cutoff = DateTime.UtcNow.Subtract(Ttl);
    foreach (var pair in _pending.Where(pair => pair.Value.RequestedAt < cutoff).ToList())
      _pending.TryRemove(pair.Key, out _);
    foreach (var pair in _consumed.Where(pair => pair.Value < cutoff).ToList())
      _consumed.TryRemove(pair.Key, out _);
  }

  private void EnforceMaxPendingActions()
  {
    if (_pending.Count <= MaxPendingActions) return;
    foreach (var key in _pending
      .OrderBy(pair => pair.Value.RequestedAt)
      .Take(_pending.Count - MaxPendingActions)
      .Select(pair => pair.Key)
      .ToList())
    {
      _pending.TryRemove(key, out _);
    }
  }
}
