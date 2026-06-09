using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin promo code management service.</summary>
public interface IAdminPromoService
{
  /// <summary>List promo codes for admin.</summary>
  Task<(IReadOnlyList<AdminPromoListItemResponse> Items, int TotalItem)> GetAllAdminAsync(
    bool includeDeleted = false,
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default);

  /// <summary>Get a single promo code for admin editing.</summary>
  Task<AdminPromoDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Create a new promo code.</summary>
  Task<AdminPromoDetailResponse> CreatePromoAsync(CreatePromoRequest request, CancellationToken cancellationToken = default);

  /// <summary>Update an existing promo code.</summary>
  Task<AdminPromoDetailResponse?> UpdateAsync(Guid id, UpdatePromoRequest request, CancellationToken cancellationToken = default);

  /// <summary>Soft-delete a promo code.</summary>
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Restore a soft-deleted promo code.</summary>
  Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Toggle promo active status.</summary>
  Task<bool> ToggleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

  /// <summary>List all promo codes for AI agent compatibility.</summary>
  Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default);

  /// <summary>Create a new promo code for AI agent compatibility.</summary>
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
