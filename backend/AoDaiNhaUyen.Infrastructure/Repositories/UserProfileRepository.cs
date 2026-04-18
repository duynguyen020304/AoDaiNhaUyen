using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class UserProfileRepository(AppDbContext dbContext) : IUserProfileRepository
{
    public async Task<UserProfileDto?> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                u.Gender,
                u.DateOfBirth,
                u.AvatarUrl,
                u.Status,
                u.EmailVerifiedAt,
                u.PhoneVerifiedAt,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<IReadOnlyList<UserAddressDto>> GetUserAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserAddresses
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.IsDefault)
            .ThenByDescending(ua => ua.CreatedAt)
            .Select(ua => new UserAddressDto(
                ua.Id,
                ua.UserId,
                ua.RecipientName,
                ua.RecipientPhone,
                ua.Province,
                ua.District,
                ua.Ward,
                ua.AddressLine,
                ua.IsDefault,
                ua.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserOrderDto>> GetUserOrdersAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new UserOrderDto(
                o.Id,
                o.OrderCode,
                o.RecipientName,
                o.RecipientPhone,
                o.Province,
                o.District,
                o.Ward,
                o.AddressLine,
                o.Subtotal,
                o.DiscountAmount,
                o.ShippingFee,
                o.TotalAmount,
                o.OrderStatus,
                o.Note,
                o.PlacedAt,
                o.ConfirmedAt,
                o.CompletedAt,
                o.CancelledAt,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(oi => new OrderItemDto(
                    oi.Id,
                    oi.ProductId,
                    oi.VariantId,
                    oi.ProductName,
                    oi.Sku,
                    oi.Size,
                    oi.Color,
                    oi.UnitPrice,
                    oi.Quantity,
                    oi.LineTotal,
                    oi.IsCustomTailoring,
                    oi.MeasurementProfileId,
                    oi.CustomMeasurementsJson,
                    oi.Note)).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserOrderDto>(orders, totalCount, page, pageSize);
    }
}