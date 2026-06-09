using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Promo code row returned to admin list pages.</summary>
public sealed record AdminPromoListItemResponse(
  Guid Id,
  string Code,
  string DiscountType,
  decimal DiscountValue,
  decimal MinOrderAmount,
  int MaxUses,
  int CurrentUses,
  bool IsActive,
  bool IsDeleted,
  bool FreeShipping,
  DateTimeOffset StartDate,
  DateTimeOffset EndDate,
  DateTimeOffset CreatedAt);

/// <summary>Promo code detail returned to admin edit pages.</summary>
public sealed record AdminPromoDetailResponse(
  Guid Id,
  string Code,
  string DiscountType,
  decimal DiscountValue,
  decimal MinOrderAmount,
  int MaxUses,
  int CurrentUses,
  bool IsActive,
  bool IsDeleted,
  bool FreeShipping,
  DateTimeOffset StartDate,
  DateTimeOffset EndDate,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);

/// <summary>Payload for creating a promo code.</summary>
public sealed record CreatePromoRequest
{
  [Required, MaxLength(50)]
  public required string Code { get; init; }

  [Required, RegularExpression("^(percentage|fixed)$")]
  public required string DiscountType { get; init; }

  [Range(0.01, 99999999)]
  public required decimal DiscountValue { get; init; }

  [Range(0, 99999999)]
  public decimal MinOrderAmount { get; init; }

  [Range(0, int.MaxValue)]
  public int MaxUses { get; init; }

  public DateTime? StartDate { get; init; }

  public DateTime? EndDate { get; init; }

  public bool FreeShipping { get; init; }

  public bool IsActive { get; init; } = true;
}

/// <summary>Payload for updating a promo code.</summary>
public sealed record UpdatePromoRequest
{
  [Required, MaxLength(50)]
  public required string Code { get; init; }

  [Required, RegularExpression("^(percentage|fixed)$")]
  public required string DiscountType { get; init; }

  [Range(0.01, 99999999)]
  public required decimal DiscountValue { get; init; }

  [Range(0, 99999999)]
  public decimal MinOrderAmount { get; init; }

  [Range(0, int.MaxValue)]
  public int MaxUses { get; init; }

  public DateTime? StartDate { get; init; }

  public DateTime? EndDate { get; init; }

  public bool FreeShipping { get; init; }

  public bool IsActive { get; init; }
}

/// <summary>Payload for toggling promo status.</summary>
public sealed record TogglePromoStatusRequest
{
  public bool IsActive { get; init; }
}
