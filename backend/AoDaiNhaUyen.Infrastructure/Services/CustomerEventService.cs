using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CustomerEventService(AppDbContext dbContext) : ICustomerEventService
{
  private static readonly HashSet<string> AllowedEvents = new(StringComparer.OrdinalIgnoreCase)
  {
    "viewed_product",
    "added_to_cart",
    "checkout_started",
    "checkout_completed",
    "promo_validated",
    "promo_applied",
    "ai_tryon_started",
    "ai_tryon_completed"
  };

  public async Task<TrackCustomerEventResultDto> TrackAsync(
    Guid? userId,
    TrackCustomerEventRequest request,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    var eventType = request.EventType.Trim().ToLowerInvariant();
    if (!AllowedEvents.Contains(eventType))
    {
      throw new ArgumentException("Loại sự kiện không hợp lệ.", nameof(request));
    }

    if (!string.IsNullOrWhiteSpace(request.MetadataJson) && request.MetadataJson.Length > 4000)
    {
      throw new ArgumentException("Dữ liệu sự kiện quá lớn.", nameof(request));
    }

    if (!string.IsNullOrWhiteSpace(request.MetadataJson))
    {
      JsonDocument.Parse(request.MetadataJson);
    }

    var now = DateTime.UtcNow;
    var occurredAt = ClampOccurredAt(request.OccurredAt, now);
    var customerEvent = new CustomerEvent
    {
      UserId = userId,
      AnonymousSessionId = string.IsNullOrWhiteSpace(request.AnonymousSessionId) ? null : request.AnonymousSessionId.Trim(),
      EventType = eventType,
      ProductId = request.ProductId,
      ProductVariantId = request.ProductVariantId,
      OrderId = request.OrderId,
      PromoCodeId = request.PromoCodeId,
      CampaignId = request.CampaignId,
      CampaignSendId = request.CampaignSendId,
      Source = TrimOrNull(request.Source),
      Medium = TrimOrNull(request.Medium),
      Campaign = TrimOrNull(request.Campaign),
      MetadataJson = request.MetadataJson,
      OccurredAt = occurredAt,
      IpHash = HashOrNull(ipAddress),
      UserAgentHash = HashOrNull(userAgent)
    };

    dbContext.CustomerEvents.Add(customerEvent);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new TrackCustomerEventResultDto(customerEvent.Id, customerEvent.EventType, customerEvent.OccurredAt);
  }

  private static DateTime ClampOccurredAt(DateTime? requested, DateTime now)
  {
    if (!requested.HasValue) return now;

    var earliest = now.AddDays(-30);
    var latest = now.AddMinutes(5);
    if (requested.Value < earliest) return earliest;
    if (requested.Value > latest) return latest;
    return requested.Value;
  }

  private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static string? HashOrNull(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
  }
}
