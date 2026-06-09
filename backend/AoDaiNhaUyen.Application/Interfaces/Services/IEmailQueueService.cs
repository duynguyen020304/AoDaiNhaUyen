using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IEmailQueueService
{
  Task<Guid> QueueAsync(
    string toEmail,
    string templateKey,
    object payload,
    DateTime? scheduledAt = null,
    CancellationToken cancellationToken = default);

  EmailJob Enqueue(
    string toEmail,
    string templateKey,
    object payload,
    DateTime? scheduledAt = null);
}
