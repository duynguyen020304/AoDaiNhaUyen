using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class CartRepository(AppDbContext dbContext, IImageVisibilityService imageVisibilityService) : ICartRepository
{
  public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    return await dbContext.Carts
      .FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
  }

  public async Task<Cart?> GetByUserIdWithItemsAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    return await dbContext.Carts
      .Include(cart => cart.Items)
        .ThenInclude(item => item.Variant)
          .ThenInclude(variant => variant.Product)
      .Include(cart => cart.Items)
        .ThenInclude(item => item.Variant)
          .ThenInclude(variant => variant.Images)
      .Include(cart => cart.Items)
        .ThenInclude(item => item.Variant)
          .ThenInclude(variant => variant.Product)
            .ThenInclude(product => product.Images)
      .FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
  }

  public async Task<CartItem?> GetItemByIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
  {
    return await dbContext.CartItems
      .Include(item => item.Cart)
      .Include(item => item.Variant)
        .ThenInclude(variant => variant.Product)
      .Include(item => item.Variant)
        .ThenInclude(variant => variant.Images)
      .Include(item => item.Variant)
        .ThenInclude(variant => variant.Product)
          .ThenInclude(product => product.Images)
      .FirstOrDefaultAsync(item => item.Id == itemId && item.Cart.UserId == userId, cancellationToken);
  }

  public async Task<ProductVariant?> GetVariantForCartAsync(Guid variantId, CancellationToken cancellationToken = default)
  {
    return await dbContext.ProductVariants
      .Include(variant => variant.Product)
        .ThenInclude(product => product.Images)
      .Include(variant => variant.Images)
      .FirstOrDefaultAsync(variant => variant.Id == variantId, cancellationToken);
  }

  public async Task<Cart> CreateForUserAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    var cart = new Cart
    {
      UserId = userId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    dbContext.Carts.Add(cart);
    await dbContext.SaveChangesAsync(cancellationToken);
    return cart;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<CartDto> MapCartAsync(Cart cart, CancellationToken cancellationToken = default)
  {
    var items = new List<CartItemDto>();
    foreach (var item in cart.Items.OrderBy(i => i.CreatedAt))
    {
      var variant = item.Variant;
      var primaryImage =
        variant.Images.OrderBy(image => image.SortOrder).FirstOrDefault(image => image.IsPrimary) ??
        variant.Images.OrderBy(image => image.SortOrder).FirstOrDefault() ??
        variant.Product.Images.OrderBy(image => image.SortOrder).FirstOrDefault(image => image.IsPrimary) ??
        variant.Product.Images.OrderBy(image => image.SortOrder).FirstOrDefault();

      var imageUrl = primaryImage is not null
        ? await imageVisibilityService.ResolveUrlAsync(primaryImage.ImageUrl, primaryImage.IsPublic, primaryImage.PublicObjectKey, cancellationToken)
        : null;

      var activePrice = variant.SalePrice ?? variant.Price;

      items.Add(new CartItemDto(
        item.Id,
        variant.Id,
        variant.ProductId,
        variant.Product.Name,
        variant.Product.Slug,
        variant.Sku,
        variant.VariantName,
        variant.Size,
        variant.Color,
        imageUrl,
        variant.Price,
        variant.SalePrice,
        item.Quantity,
        activePrice * item.Quantity));
    }

    return new CartDto(
      cart.Id,
      cart.UserId,
      items.Sum(item => item.Quantity),
      items.Sum(item => item.LineTotal),
      items);
  }
}
