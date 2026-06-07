namespace AoDaiNhaUyen.Application.DTOs.Promo;

/// <summary>
/// Kết quả validate mã giảm giá.
/// </summary>
public sealed record PromoValidationResult(
  bool IsValid,
  string? ErrorCode,
  string? ErrorMessage,
  decimal DiscountAmount,
  bool FreeShipping,
  string? DiscountLabel);

/// <summary>
/// Yêu cầu áp dụng mã giảm giá.
/// </summary>
public sealed record ApplyPromoRequest(
  string Code,
  decimal Subtotal);
