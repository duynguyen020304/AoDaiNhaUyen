using AoDaiNhaUyen.Application.DTOs;
namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStylistChatService
{
  Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> CreateThreadAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> GetThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default);

  Task<ChatThreadDetailDto> AddMessageAsync(
    long threadId,
    long? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken = default);

  Task<ChatMessageDto> ExecuteTryOnAsync(
    long threadId,
    long? userId,
    string? guestKey,
    long? garmentProductId,
    IReadOnlyList<long> accessoryProductIds,
    CancellationToken cancellationToken = default);
}
