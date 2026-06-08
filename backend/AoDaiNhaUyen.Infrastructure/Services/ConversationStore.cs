using System.Collections.Concurrent;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ConversationStore : IConversationStore
{
  private const int MaxConversations = 200;
  private static readonly TimeSpan Ttl = TimeSpan.FromHours(2);
  private readonly ConcurrentDictionary<string, ConversationEntry> _conversations = new();

  public (List<AdminLlmMessage> History, Guid AdminUserId) GetOrAdd(
    string conversationId, Func<(List<AdminLlmMessage>, Guid)> factory)
  {
    PruneExpired();
    var entry = _conversations.GetOrAdd(conversationId, _ =>
    {
      var (history, adminUserId) = factory();
      return new ConversationEntry(history, adminUserId, DateTime.UtcNow);
    });
    entry.LastAccessedUtc = DateTime.UtcNow;
    EnforceMaxConversations();
    return (entry.History, entry.AdminUserId);
  }

  public void Touch(string conversationId)
  {
    if (_conversations.TryGetValue(conversationId, out var entry))
      entry.LastAccessedUtc = DateTime.UtcNow;
  }

  public void TrimHistory(string conversationId, int maxTurns)
  {
    if (!_conversations.TryGetValue(conversationId, out var entry)) return;
    var maxMessages = Math.Max(2, maxTurns * 2);
    lock (entry.History)
    {
      if (entry.History.Count <= maxMessages) return;
      entry.History.RemoveRange(0, entry.History.Count - maxMessages);
    }
  }

  public void Remove(string conversationId) =>
    _conversations.TryRemove(conversationId, out _);

  public bool TryGetValue(string conversationId, out (List<AdminLlmMessage> History, Guid AdminUserId) value)
  {
    PruneExpired();
    if (_conversations.TryGetValue(conversationId, out var entry))
    {
      entry.LastAccessedUtc = DateTime.UtcNow;
      value = (entry.History, entry.AdminUserId);
      return true;
    }

    value = default;
    return false;
  }

  private void PruneExpired()
  {
    var cutoff = DateTime.UtcNow.Subtract(Ttl);
    foreach (var pair in _conversations.Where(pair => pair.Value.LastAccessedUtc < cutoff).ToList())
      _conversations.TryRemove(pair.Key, out _);
  }

  private void EnforceMaxConversations()
  {
    if (_conversations.Count <= MaxConversations) return;
    foreach (var key in _conversations
      .OrderBy(pair => pair.Value.LastAccessedUtc)
      .Take(_conversations.Count - MaxConversations)
      .Select(pair => pair.Key)
      .ToList())
    {
      _conversations.TryRemove(key, out _);
    }
  }

  private sealed class ConversationEntry(List<AdminLlmMessage> history, Guid adminUserId, DateTime lastAccessedUtc)
  {
    public List<AdminLlmMessage> History { get; } = history;
    public Guid AdminUserId { get; } = adminUserId;
    public DateTime LastAccessedUtc { get; set; } = lastAccessedUtc;
  }
}
