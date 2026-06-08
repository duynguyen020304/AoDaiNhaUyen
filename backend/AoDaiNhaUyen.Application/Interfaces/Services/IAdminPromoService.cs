namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin promo code management service for AI agent.</summary>
public interface IAdminPromoService
{
  /// <summary>List all promo codes.</summary>
  Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default);

  /// <summary>Create a new promo code.</summary>
  Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default);
}

public sealed record AdminPromoItem(
  Guid Id,
  string Code,
  string DiscountType,
  decimal DiscountValue,
  decimal MinOrderAmount,
  int MaxUses,
  int CurrentUses,
  bool IsActive,
  DateTime StartDate,
  DateTime EndDate);

public sealed record CreateAdminPromoRequest(
  string Code,
  string DiscountType,
  decimal DiscountValue,
  decimal MinOrderAmount = 0,
  int MaxUses = 0,
  DateTime? StartDate = null,
  DateTime? EndDate = null);

public sealed record AdminPromoResult(
  bool Success,
  string Message,
  Guid? PromoId);
