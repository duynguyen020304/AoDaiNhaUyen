using System.Collections.Concurrent;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class PendingActionStore : IPendingActionStore
{
  private readonly ConcurrentDictionary<string, AdminPendingAction> _pending = new();

  public void Add(string actionId, AdminPendingAction pending) =>
    _pending[actionId] = pending;

  public AdminPendingAction? Remove(string actionId) =>
    _pending.TryRemove(actionId, out var p) ? p : null;
}
