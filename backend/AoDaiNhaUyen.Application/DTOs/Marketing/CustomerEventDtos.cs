using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Marketing;

public sealed record TrackCustomerEventRequest
{
  [Required, MaxLength(60)]
  public required string EventType { get; init; }

  [MaxLength(128)]
  public string? AnonymousSessionId { get; init; }

  public Guid? ProductId { get; init; }
  public Guid? ProductVariantId { get; init; }
  public Guid? OrderId { get; init; }
  public Guid? PromoCodeId { get; init; }
  public Guid? CampaignId { get; init; }
  public Guid? CampaignSendId { get; init; }

  [MaxLength(80)]
  public string? Source { get; init; }

  [MaxLength(80)]
  public string? Medium { get; init; }

  [MaxLength(120)]
  public string? Campaign { get; init; }

  public string? MetadataJson { get; init; }
  public DateTime? OccurredAt { get; init; }
}

public sealed record TrackCustomerEventResultDto(Guid Id, string EventType, DateTime OccurredAt);
