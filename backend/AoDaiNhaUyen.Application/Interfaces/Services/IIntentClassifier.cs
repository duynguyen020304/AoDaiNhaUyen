using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IIntentClassifier
{
  Task<IntentClassificationDto> ClassifyAsync(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken = default);
}
