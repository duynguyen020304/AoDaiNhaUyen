using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Thread-safe store for pending AI actions that need admin confirmation.
/// Lives as a singleton so both the SSE chat stream and the confirm endpoint
/// see the same state across separate HTTP requests.</summary>
public interface IPendingActionStore
{
  void Add(string actionId, AdminPendingAction pending);
  AdminPendingAction? Remove(string actionId);
}
