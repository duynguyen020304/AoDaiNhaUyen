using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class HermesEventOutboxPublisher(
  AppDbContext dbContext,
  IOptions<HermesOutboxOptions> options) : IHermesEventOutboxPublisher, IHermesEventOutboxService
{
  private const int MaxPageSize = 100;
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
  {
    "pending", "processing", "completed", "failed", "dead", "cancelled"
  };

  private readonly HermesOutboxOptions _options = options.Value;

  public async Task<HermesEventOutboxResponse?> EnqueueAsync(HermesEventOutboxRequest request, CancellationToken cancellationToken)
  {
    var payloadJson = NormalizePayloadJson(request.PayloadJson, _options.MaxPayloadBytes);
    var idempotencyKey = Limit(request.IdempotencyKey, 200);

    if (!string.IsNullOrWhiteSpace(idempotencyKey) && await dbContext.HermesEventOutbox.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
    {
      return null;
    }

    var now = DateTimeOffset.UtcNow;
    var entity = new HermesEventOutbox
    {
      Id = Guid.NewGuid(),
      EventType = LimitRequired(request.EventType, 100).ToLowerInvariant(),
      AggregateType = LimitRequired(request.AggregateType, 80),
      AggregateId = LimitRequired(request.AggregateId, 128),
      PayloadJson = payloadJson,
      Status = "pending",
      Attempts = 0,
      MaxAttempts = Math.Clamp(request.MaxAttempts ?? _options.MaxAttempts, 1, 20),
      CorrelationId = Limit(request.CorrelationId, 128),
      IdempotencyKey = idempotencyKey,
      OccurredAt = request.OccurredAt ?? now,
      ScheduledAt = request.ScheduledAt ?? now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    };

    dbContext.HermesEventOutbox.Add(entity);
    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(idempotencyKey))
    {
      return null;
    }

    return Map(entity);
  }

  public Task EnqueueAdminEventAsync(string eventType, string aggregateType, string aggregateId, object payload, string? idempotencyKey = null, string? correlationId = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, aggregateType, aggregateId, payload, idempotencyKey, correlationId, cancellationToken);

  public Task EnqueueAdminOrderEventAsync(string eventType, Guid orderId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Order", orderId.ToString("N"), payload, idempotencyKey, orderId.ToString("N"), cancellationToken);

  public Task EnqueueAdminProductEventAsync(string eventType, Guid productId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Product", productId.ToString("N"), payload, idempotencyKey, productId.ToString("N"), cancellationToken);

  public Task EnqueueAdminInventoryEventAsync(string eventType, Guid variantId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Inventory", variantId.ToString("N"), payload, idempotencyKey, variantId.ToString("N"), cancellationToken);

  public Task EnqueueAdminPromotionEventAsync(string eventType, Guid promoId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Promotion", promoId.ToString("N"), payload, idempotencyKey, promoId.ToString("N"), cancellationToken);

  public Task EnqueueAdminSecurityEventAsync(string eventType, Guid targetId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "AdminSecurity", targetId.ToString("N"), payload, idempotencyKey, targetId.ToString("N"), cancellationToken);

  public Task EnqueueAdminContentEventAsync(string eventType, Guid contentId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Content", contentId.ToString("N"), payload, idempotencyKey, contentId.ToString("N"), cancellationToken);

  public Task EnqueueAdminEmailEventAsync(string eventType, Guid emailResourceId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "Email", emailResourceId.ToString("N"), payload, idempotencyKey, emailResourceId.ToString("N"), cancellationToken);

  public Task EnqueueAdminAiConfigEventAsync(string eventType, string aggregateId, object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    => EnqueueAndDiscardAsync(eventType, "HermesConfig", aggregateId, payload, idempotencyKey, aggregateId, cancellationToken);

  public async Task<PagedResult<HermesEventOutboxListItemResponse>> ListEventsAsync(HermesEventOutboxSearchRequest request, CancellationToken cancellationToken)
  {
    var page = Math.Max(1, request.Page);
    var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
    var query = dbContext.HermesEventOutbox.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(request.Status))
    {
      var status = request.Status.Trim().ToLowerInvariant();
      if (!AllowedStatuses.Contains(status)) throw new ArgumentException("Trạng thái event Hermes không hợp lệ.", nameof(request.Status));
      query = query.Where(x => x.Status == status);
    }

    if (!string.IsNullOrWhiteSpace(request.EventType))
    {
      var eventType = request.EventType.Trim().ToLowerInvariant();
      query = query.Where(x => x.EventType == eventType);
    }

    if (!string.IsNullOrWhiteSpace(request.AggregateType))
    {
      var aggregateType = request.AggregateType.Trim();
      query = query.Where(x => x.AggregateType == aggregateType);
    }

    if (!string.IsNullOrWhiteSpace(request.Q))
    {
      var q = request.Q.Trim();
      query = query.Where(x => x.AggregateId.Contains(q) || (x.CorrelationId != null && x.CorrelationId.Contains(q)) || (x.IdempotencyKey != null && x.IdempotencyKey.Contains(q)));
    }

    var total = await query.CountAsync(cancellationToken);
    var items = await query
      .OrderByDescending(x => x.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(x => new HermesEventOutboxListItemResponse(
        x.Id,
        x.EventType,
        x.AggregateType,
        x.AggregateId,
        x.Status,
        x.Attempts,
        x.MaxAttempts,
        x.LastError,
        x.CorrelationId,
        x.IdempotencyKey,
        x.OccurredAt,
        x.ScheduledAt,
        x.ProcessedAt,
        x.CreatedAt))
      .ToListAsync(cancellationToken);

    return new PagedResult<HermesEventOutboxListItemResponse>(items, total, page, pageSize);
  }

  public async Task<HermesEventOutboxResponse?> GetEventAsync(Guid id, CancellationToken cancellationToken)
  {
    var entity = await dbContext.HermesEventOutbox.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    return entity is null ? null : Map(entity);
  }

  public async Task<bool> RetryEventAsync(Guid id, CancellationToken cancellationToken)
  {
    var entity = await dbContext.HermesEventOutbox.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (entity is null || entity.Status is "pending" or "processing") return false;

    entity.Status = "pending";
    entity.LastError = null;
    entity.LockedAt = null;
    entity.LockedBy = null;
    entity.ScheduledAt = DateTimeOffset.UtcNow;
    entity.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  public async Task<bool> CancelEventAsync(Guid id, CancellationToken cancellationToken)
  {
    var entity = await dbContext.HermesEventOutbox.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (entity is null || entity.Status is "completed" or "dead") return false;

    entity.Status = "cancelled";
    entity.LockedAt = null;
    entity.LockedBy = null;
    entity.ProcessedAt = DateTimeOffset.UtcNow;
    entity.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  private async Task EnqueueAndDiscardAsync(string eventType, string aggregateType, string aggregateId, object payload, string? idempotencyKey, string? correlationId, CancellationToken cancellationToken)
  {
    var json = JsonSerializer.Serialize(payload, JsonOptions);
    await EnqueueAsync(new HermesEventOutboxRequest
    {
      EventType = eventType,
      AggregateType = aggregateType,
      AggregateId = aggregateId,
      PayloadJson = json,
      CorrelationId = correlationId,
      IdempotencyKey = idempotencyKey
    }, cancellationToken);
  }

  private static HermesEventOutboxResponse Map(HermesEventOutbox entity) =>
    new(
      entity.Id,
      entity.EventType,
      entity.AggregateType,
      entity.AggregateId,
      entity.PayloadJson,
      entity.Status,
      entity.Attempts,
      entity.MaxAttempts,
      entity.LastError,
      entity.CorrelationId,
      entity.IdempotencyKey,
      entity.LockedBy,
      entity.LockedAt,
      entity.OccurredAt,
      entity.ScheduledAt,
      entity.ProcessedAt,
      entity.CreatedAt,
      entity.UpdatedAt);

  private static string NormalizePayloadJson(string payloadJson, int maxBytes)
  {
    if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("PayloadJson bắt buộc.", nameof(payloadJson));
    var trimmed = payloadJson.Trim();
    if (maxBytes > 0 && System.Text.Encoding.UTF8.GetByteCount(trimmed) > maxBytes) throw new ArgumentException("PayloadJson quá dài.", nameof(payloadJson));
    using var _ = JsonDocument.Parse(trimmed);
    return trimmed;
  }

  private static string LimitRequired(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Thiếu dữ liệu bắt buộc.", nameof(value));
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }

  private static string? Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }
}
