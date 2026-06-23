using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class UserProfileRepository(AppDbContext dbContext, IImageVisibilityService imageVisibilityService) : IUserProfileRepository
{
    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<UserAddressDto>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
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

    public async Task<PagedResult<UserOrderDto>> GetUserOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Payment)
            .Include(o => o.Items)
              .ThenInclude(oi => oi.Product)
                .ThenInclude(product => product!.Images)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var orderEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var orders = new List<UserOrderDto>();
        foreach (var o in orderEntities)
        {
            var items = new List<OrderItemDto>();
            foreach (var oi in o.Items)
            {
                var publicProductImage = ResolveOrderItemPublicProductImage(oi);
                var imageUrl = publicProductImage is not null
                    ? await imageVisibilityService.ResolveUrlAsync(
                        publicProductImage.ImageUrl,
                        publicProductImage.IsPublic,
                        publicProductImage.PublicObjectKey,
                        cancellationToken)
                    : null;

                items.Add(new OrderItemDto(
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
                    imageUrl,
                    oi.IsCustomTailoring,
                    oi.MeasurementProfileId,
                    oi.CustomMeasurementsJson,
                    oi.Note));
            }

            orders.Add(new UserOrderDto(
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
                o.Payment != null ? "paid" : null,
                o.Note,
                o.PlacedAt,
                o.ConfirmedAt,
                o.CompletedAt,
                o.CancelledAt,
                o.CreatedAt,
                o.UpdatedAt,
                items));
        }

        return new PagedResult<UserOrderDto>(orders, totalCount, page, pageSize);
    }

    private static ProductImage? ResolveOrderItemPublicProductImage(OrderItem orderItem)
    {
        if (orderItem.Product is null)
        {
            return null;
        }

        return orderItem.Product.Images
            .Where(image => image.IsPublic && !string.IsNullOrWhiteSpace(image.PublicObjectKey))
            .OrderBy(image => image.SortOrder)
            .FirstOrDefault(image => image.IsPrimary)
            ?? orderItem.Product.Images
                .Where(image => image.IsPublic && !string.IsNullOrWhiteSpace(image.PublicObjectKey))
                .OrderBy(image => image.SortOrder)
                .FirstOrDefault();
    }
}
