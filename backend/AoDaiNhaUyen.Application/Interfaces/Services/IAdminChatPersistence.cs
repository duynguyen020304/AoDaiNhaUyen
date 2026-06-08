using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminChatPersistence
{
  Task<ChatThread> CreateThreadAsync(Guid adminUserId, string? title, CancellationToken ct);
  Task<ChatThread?> GetThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct);
  Task<List<ChatThread>> ListThreadsAsync(Guid adminUserId, CancellationToken ct);
  Task<ChatMessage?> AddMessageAsync(Guid threadId, Guid adminUserId, string role, string content,
    string? toolCallsJson, string? structuredPayloadJson, CancellationToken ct);
  Task<List<ChatMessage>> GetMessagesAsync(Guid threadId, Guid adminUserId, CancellationToken ct);
  Task<bool> DeleteThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct);
}
