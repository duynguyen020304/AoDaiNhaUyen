using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminPromoService(AppDbContext dbContext) : IAdminPromoService
{
  public async Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default)
  {
    return await dbContext.PromoCodes
      .AsNoTracking()
      .Where(p => !p.IsDeleted)
      .OrderByDescending(p => p.CreatedAt)
      .Select(p => new AdminPromoItem(
        p.Id,
        p.Code,
        p.DiscountType,
        p.DiscountValue,
        p.MinOrderAmount,
        p.MaxUses,
        p.CurrentUses,
        p.IsActive,
        p.StartDate,
        p.EndDate))
      .ToListAsync(ct);
  }

  public async Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default)
  {
    var code = request.Code.Trim().ToUpperInvariant();

    // Check duplicate
    var exists = await dbContext.PromoCodes
      .AnyAsync(p => p.Code == code && !p.IsDeleted, ct);

    if (exists)
      return new AdminPromoResult(false, $"Mã '{code}' đã tồn tại.", null);

    if (request.DiscountType is not ("percentage" or "fixed"))
      return new AdminPromoResult(false, "Loại giảm giá phải là 'percentage' hoặc 'fixed'.", null);

    if (request.DiscountValue <= 0)
      return new AdminPromoResult(false, "Giá trị giảm giá phải lớn hơn 0.", null);

    var now = DateTime.UtcNow;
    var promo = new PromoCode
    {
      Code = code,
      DiscountType = request.DiscountType,
      DiscountValue = request.DiscountValue,
      MinOrderAmount = request.MinOrderAmount,
      MaxUses = request.MaxUses,
      CurrentUses = 0,
      IsActive = true,
      StartDate = request.StartDate ?? now,
      EndDate = request.EndDate ?? now.AddDays(30),
      CreatedAt = now
    };

    dbContext.PromoCodes.Add(promo);
    await dbContext.SaveChangesAsync(ct);

    return new AdminPromoResult(true, $"Đã tạo mã '{code}' ({request.DiscountType}: {request.DiscountValue}).", promo.Id);
  }
}
