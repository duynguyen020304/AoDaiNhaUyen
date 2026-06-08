using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Thread-safe store for admin AI conversation histories.
/// Lives as a singleton so both the SSE chat stream and the confirm endpoint
/// see the same conversation state across separate HTTP requests.</summary>
public interface IConversationStore
{
  (List<AdminLlmMessage> History, Guid AdminUserId) GetOrAdd(string conversationId, Func<(List<AdminLlmMessage>, Guid)> factory);
  void Touch(string conversationId);
  void TrimHistory(string conversationId, int maxTurns);
  void Remove(string conversationId);
  bool TryGetValue(string conversationId, out (List<AdminLlmMessage> History, Guid AdminUserId) value);
}
