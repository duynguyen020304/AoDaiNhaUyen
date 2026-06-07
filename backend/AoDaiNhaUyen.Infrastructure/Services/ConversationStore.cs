using System.Collections.Concurrent;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ConversationStore : IConversationStore
{
  private readonly ConcurrentDictionary<string, (List<AdminLlmMessage> History, Guid AdminUserId)> _conversations = new();

  public (List<AdminLlmMessage> History, Guid AdminUserId) GetOrAdd(
    string conversationId, Func<(List<AdminLlmMessage>, Guid)> factory) =>
    _conversations.GetOrAdd(conversationId, _ => factory());

  public void Remove(string conversationId) =>
    _conversations.TryRemove(conversationId, out _);

  public bool TryGetValue(string conversationId, out (List<AdminLlmMessage> History, Guid AdminUserId) value) =>
    _conversations.TryGetValue(conversationId, out value);
}
