using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminChatPersistence(AppDbContext dbContext) : IAdminChatPersistence
{
  public async Task<ChatThread> CreateThreadAsync(Guid adminUserId, string? title, CancellationToken ct)
  {
    var thread = new ChatThread
    {
      UserId = adminUserId,
      Source = ChatSources.AdminAi,
      Status = "active",
      GuestKeyHash = null
    };

    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync(ct);
    return thread;
  }

  public Task<ChatThread?> GetThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct) =>
    dbContext.ChatThreads
      .FirstOrDefaultAsync(t => t.Id == threadId
        && t.UserId == adminUserId
        && t.Source == ChatSources.AdminAi
        && !t.IsDeleted, ct);

  public Task<List<ChatThread>> ListThreadsAsync(Guid adminUserId, CancellationToken ct) =>
    dbContext.ChatThreads
      .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
      .Where(t => t.UserId == adminUserId && t.Source == ChatSources.AdminAi && !t.IsDeleted)
      .OrderByDescending(t => t.UpdatedAt)
      .Take(50)
      .ToListAsync(ct);

  public async Task<ChatMessage?> AddMessageAsync(Guid threadId, Guid adminUserId, string role, string content,
    string? toolCallsJson, string? structuredPayloadJson, CancellationToken ct)
  {
    var threadExists = await dbContext.ChatThreads
      .AsNoTracking()
      .AnyAsync(t => t.Id == threadId
        && t.UserId == adminUserId
        && t.Source == ChatSources.AdminAi
        && !t.IsDeleted, ct);
    if (!threadExists) return null;

    var message = new ChatMessage
    {
      ThreadId = threadId,
      Role = role,
      Content = content,
      ToolCallsJsonb = toolCallsJson,
      StructuredPayloadJsonb = structuredPayloadJson
    };

    dbContext.ChatMessages.Add(message);

    try
    {
      await dbContext.SaveChangesAsync(ct);
    }
    catch (DbUpdateException)
    {
      dbContext.Entry(message).State = EntityState.Detached;
      return null;
    }

    await dbContext.ChatThreads
      .Where(t => t.Id == threadId
        && t.UserId == adminUserId
        && t.Source == ChatSources.AdminAi
        && !t.IsDeleted)
      .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);

    return message;
  }

  public Task<List<ChatMessage>> GetMessagesAsync(Guid threadId, Guid adminUserId, CancellationToken ct) =>
    dbContext.ChatMessages
      .Where(m => m.ThreadId == threadId
        && m.Thread.UserId == adminUserId
        && m.Thread.Source == ChatSources.AdminAi
        && !m.Thread.IsDeleted)
      .OrderBy(m => m.CreatedAt)
      .ToListAsync(ct);

  public async Task<bool> DeleteThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct)
  {
    var thread = await GetThreadAsync(threadId, adminUserId, ct);
    if (thread is null) return false;

    thread.IsDeleted = true;
    thread.DeletedAt = DateTime.UtcNow;
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);
    return true;
  }
}
