using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin promo code management service implementation.</summary>
public sealed class AdminPromoService(
  AppDbContext dbContext,
  IHermesEventOutboxPublisher hermesEvents,
  ILogger<AdminPromoService> logger) : IAdminPromoService
{
  public async Task<(IReadOnlyList<AdminPromoListItemResponse> Items, int TotalItem)> GetAllAdminAsync(
    bool includeDeleted = false,
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = dbContext.PromoCodes.AsNoTracking().AsQueryable();

    if (includeDeleted)
      query = query.IgnoreQueryFilters();

    if (!string.IsNullOrWhiteSpace(search))
    {
      var normalizedSearch = search.Trim().ToUpperInvariant();
      query = query.Where(p => p.Code.Contains(normalizedSearch));
    }

    if (isActive.HasValue)
      query = query.Where(p => p.IsActive == isActive.Value);

    var totalItem = await query.CountAsync(cancellationToken);

    var items = await query
      .OrderByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(p => new AdminPromoListItemResponse(
        p.Id,
        p.Code,
        p.DiscountType,
        p.DiscountValue,
        p.MinOrderAmount,
        p.MaxUses,
        p.CurrentUses,
        p.IsActive,
        p.IsDeleted,
        p.FreeShipping,
        new DateTimeOffset(p.StartDate, TimeSpan.Zero),
        new DateTimeOffset(p.EndDate, TimeSpan.Zero),
        new DateTimeOffset(p.CreatedAt, TimeSpan.Zero)))
      .ToListAsync(cancellationToken);

    return (items.AsReadOnly(), totalItem);
  }

  public async Task<AdminPromoDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes
      .AsNoTracking()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    return promo is null ? null : MapToDetail(promo);
  }

  public async Task<AdminPromoDetailResponse> CreatePromoAsync(CreatePromoRequest request, CancellationToken cancellationToken = default)
  {
    var now = DateTime.UtcNow;
    var code = NormalizeCode(request.Code);
    var startDate = NormalizeDate(request.StartDate ?? now);
    var endDate = NormalizeDate(request.EndDate ?? now.AddDays(30));

    await ValidatePromoAsync(code, request.DiscountType, request.DiscountValue, startDate, endDate, null, cancellationToken);

    var promo = new PromoCode
    {
      Code = code,
      DiscountType = request.DiscountType,
      DiscountValue = request.DiscountValue,
      MinOrderAmount = request.MinOrderAmount,
      MaxUses = request.MaxUses,
      CurrentUses = 0,
      IsActive = request.IsActive,
      IsDeleted = false,
      FreeShipping = request.FreeShipping,
      StartDate = startDate,
      EndDate = endDate,
      CreatedAt = now,
      UpdatedAt = now
    };

    dbContext.PromoCodes.Add(promo);
    await hermesEvents.EnqueueAdminPromotionEventAsync(
      "promo_created",
      promo.Id,
      new { promoId = promo.Id, promo.Code, promo.DiscountType, promo.DiscountValue, promo.MinOrderAmount, promo.MaxUses, promo.IsActive, promo.FreeShipping, promo.StartDate, promo.EndDate },
      $"promo_created:Promotion:{promo.Id:N}:{promo.CreatedAt.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Admin created promo code {PromoId} ({Code})", promo.Id, promo.Code);
    return MapToDetail(promo);
  }

  public async Task<AdminPromoDetailResponse?> UpdateAsync(Guid id, UpdatePromoRequest request, CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (promo is null)
    {
      logger.LogWarning("Admin attempted to update non-existent promo {PromoId}", id);
      return null;
    }

    var code = NormalizeCode(request.Code);
    var startDate = NormalizeDate(request.StartDate ?? promo.StartDate);
    var endDate = NormalizeDate(request.EndDate ?? promo.EndDate);

    await ValidatePromoAsync(code, request.DiscountType, request.DiscountValue, startDate, endDate, id, cancellationToken);

    promo.Code = code;
    promo.DiscountType = request.DiscountType;
    promo.DiscountValue = request.DiscountValue;
    promo.MinOrderAmount = request.MinOrderAmount;
    promo.MaxUses = request.MaxUses;
    promo.IsActive = request.IsActive;
    promo.FreeShipping = request.FreeShipping;
    promo.StartDate = startDate;
    promo.EndDate = endDate;
    promo.UpdatedAt = DateTime.UtcNow;

    await hermesEvents.EnqueueAdminPromotionEventAsync(
      "promo_updated",
      promo.Id,
      new { promoId = promo.Id, promo.Code, promo.DiscountType, promo.DiscountValue, promo.MinOrderAmount, promo.MaxUses, promo.IsActive, promo.FreeShipping, promo.StartDate, promo.EndDate },
      $"promo_updated:Promotion:{promo.Id:N}:{promo.UpdatedAt.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Admin updated promo code {PromoId} ({Code})", promo.Id, promo.Code);
    return MapToDetail(promo);
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (promo is null || promo.IsDeleted)
    {
      logger.LogWarning("Admin attempted to delete non-existent or already-deleted promo {PromoId}", id);
      return false;
    }

    var now = DateTime.UtcNow;
    promo.IsDeleted = true;
    promo.DeletedAt = now;
    promo.UpdatedAt = now;

    await hermesEvents.EnqueueAdminPromotionEventAsync(
      "promo_disabled",
      promo.Id,
      new { promoId = promo.Id, promo.Code, promo.IsActive, promo.IsDeleted, deletedAt = promo.DeletedAt },
      $"promo_disabled:Promotion:{promo.Id:N}:{promo.UpdatedAt.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Admin soft-deleted promo code {PromoId} ({Code})", promo.Id, promo.Code);
    return true;
  }

  public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (promo is null || !promo.IsDeleted)
    {
      logger.LogWarning("Admin attempted to restore non-existent or non-deleted promo {PromoId}", id);
      return false;
    }

    promo.IsDeleted = false;
    promo.DeletedAt = null;
    promo.UpdatedAt = DateTime.UtcNow;

    await hermesEvents.EnqueueAdminPromotionEventAsync(
      "promo_updated",
      promo.Id,
      new { promoId = promo.Id, promo.Code, action = "restored" },
      $"promo_restored:Promotion:{promo.Id:N}:{promo.UpdatedAt.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Admin restored promo code {PromoId} ({Code})", promo.Id, promo.Code);
    return true;
  }

  public async Task<bool> ToggleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
  {
    var promo = await dbContext.PromoCodes
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (promo is null || promo.IsDeleted)
    {
      logger.LogWarning("Admin attempted to toggle non-existent or deleted promo {PromoId}", id);
      return false;
    }

    promo.IsActive = isActive;
    promo.UpdatedAt = DateTime.UtcNow;

    await hermesEvents.EnqueueAdminPromotionEventAsync(
      isActive ? "promo_updated" : "promo_disabled",
      promo.Id,
      new { promoId = promo.Id, promo.Code, isActive },
      $"promo_status_changed:Promotion:{promo.Id:N}:{isActive}:{promo.UpdatedAt.Ticks}",
      cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Admin toggled promo code {PromoId} ({Code}) to active={IsActive}", promo.Id, promo.Code, isActive);
    return true;
  }

  public async Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default)
  {
    var (items, _) = await GetAllAdminAsync(false, null, null, 1, 100, ct);
    return items.Select(p => new AdminPromoItem(
      p.Id,
      p.Code,
      p.DiscountType,
      p.DiscountValue,
      p.MinOrderAmount,
      p.MaxUses,
      p.CurrentUses,
      p.IsActive,
      p.StartDate.UtcDateTime,
      p.EndDate.UtcDateTime)).ToList().AsReadOnly();
  }

  public async Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default)
  {
    try
    {
      var promo = await CreatePromoAsync(new CreatePromoRequest
      {
        Code = request.Code,
        DiscountType = request.DiscountType,
        DiscountValue = request.DiscountValue,
        MinOrderAmount = request.MinOrderAmount,
        MaxUses = request.MaxUses,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        IsActive = true
      }, ct);

      return new AdminPromoResult(true, $"Đã tạo mã '{promo.Code}'.", promo.Id);
    }
    catch (ArgumentException ex)
    {
      return new AdminPromoResult(false, ex.Message, null);
    }
    catch (InvalidOperationException ex)
    {
      return new AdminPromoResult(false, ex.Message, null);
    }
  }

  private async Task ValidatePromoAsync(
    string code,
    string discountType,
    decimal discountValue,
    DateTime startDate,
    DateTime endDate,
    Guid? currentPromoId,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(code))
      throw new ArgumentException("Mã giảm giá là bắt buộc.");

    if (discountType is not ("percentage" or "fixed"))
      throw new ArgumentException("Loại giảm giá phải là 'percentage' hoặc 'fixed'.");

    if (discountValue <= 0)
      throw new ArgumentException("Giá trị giảm giá phải lớn hơn 0.");

    if (discountType == "percentage" && discountValue > 100)
      throw new ArgumentException("Giá trị phần trăm không được vượt quá 100.");

    if (endDate <= startDate)
      throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");

    var exists = await dbContext.PromoCodes
      .IgnoreQueryFilters()
      .AnyAsync(p => p.Code == code && (!currentPromoId.HasValue || p.Id != currentPromoId.Value), cancellationToken);

    if (exists)
      throw new InvalidOperationException($"Mã '{code}' đã tồn tại.");
  }

  private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

  private static DateTime NormalizeDate(DateTime date) => date.Kind switch
  {
    DateTimeKind.Utc => date,
    DateTimeKind.Local => date.ToUniversalTime(),
    _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
  };

  private static AdminPromoDetailResponse MapToDetail(PromoCode promo) =>
    new(
      promo.Id,
      promo.Code,
      promo.DiscountType,
      promo.DiscountValue,
      promo.MinOrderAmount,
      promo.MaxUses,
      promo.CurrentUses,
      promo.IsActive,
      promo.IsDeleted,
      promo.FreeShipping,
      new DateTimeOffset(promo.StartDate, TimeSpan.Zero),
      new DateTimeOffset(promo.EndDate, TimeSpan.Zero),
      new DateTimeOffset(promo.CreatedAt, TimeSpan.Zero),
      new DateTimeOffset(promo.UpdatedAt, TimeSpan.Zero));
}
