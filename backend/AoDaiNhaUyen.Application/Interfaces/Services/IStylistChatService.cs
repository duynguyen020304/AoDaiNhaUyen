using AoDaiNhaUyen.Application.DTOs;
namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStylistChatService
{
  Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
    Guid? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> CreateThreadAsync(
    Guid? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> GetThreadAsync(
    Guid threadId,
    Guid? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> AddMessageAsync(
    Guid threadId,
    Guid? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken = default);

  IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(
    Guid threadId,
    Guid? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken = default);

  Task<ChatMessageDto> ExecuteTryOnAsync(
    Guid threadId,
    Guid? userId,
    string? guestKey,
    Guid? garmentProductId,
    IReadOnlyList<Guid> accessoryProductIds,
    CancellationToken cancellationToken = default);
}
