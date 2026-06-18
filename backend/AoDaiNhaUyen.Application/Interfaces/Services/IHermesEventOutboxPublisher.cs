using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesEventOutboxPublisher
{
  Task<HermesEventOutboxResponse?> EnqueueAsync(HermesEventOutboxRequest request, CancellationToken cancellationToken);
  Task EnqueueAdminEventAsync(string eventType, string aggregateType, string aggregateId, object payload, string? idempotencyKey = null, string? correlationId = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminOrderEventAsync(string eventType, Guid orderId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminProductEventAsync(string eventType, Guid productId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminInventoryEventAsync(string eventType, Guid variantId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminPromotionEventAsync(string eventType, Guid promoId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminSecurityEventAsync(string eventType, Guid targetId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminContentEventAsync(string eventType, Guid contentId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminEmailEventAsync(string eventType, Guid emailResourceId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
  Task EnqueueAdminAiConfigEventAsync(string eventType, string aggregateId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default);
}
