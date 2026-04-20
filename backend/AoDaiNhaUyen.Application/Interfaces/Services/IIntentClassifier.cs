using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IIntentClassifier
{
  Task<IntentClassificationDto> ClassifyAsync(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    string? previousUserMessage = null,
    string? previousAssistantMessage = null,
    CancellationToken cancellationToken = default);
}
