using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class CartRepository(AppDbContext dbContext) : ICartRepository
{
  public async Task<Cart?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
  {
    return await dbContext.Carts
      .FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
  }

  public async Task<Cart?> GetByUserIdWithItemsAsync(long userId, CancellationToken cancellationToken = default)
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

  public async Task<CartItem?> GetItemByIdAsync(long userId, long itemId, CancellationToken cancellationToken = default)
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

  public async Task<ProductVariant?> GetVariantForCartAsync(long variantId, CancellationToken cancellationToken = default)
  {
    return await dbContext.ProductVariants
      .Include(variant => variant.Product)
        .ThenInclude(product => product.Images)
      .Include(variant => variant.Images)
      .FirstOrDefaultAsync(variant => variant.Id == variantId, cancellationToken);
  }

  public async Task<Cart> CreateForUserAsync(long userId, CancellationToken cancellationToken = default)
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

  public CartDto MapCart(Cart cart)
  {
    var items = cart.Items
      .OrderBy(item => item.CreatedAt)
      .Select(item =>
      {
        var variant = item.Variant;
        var imageUrl =
          variant.Images.OrderBy(image => image.SortOrder).FirstOrDefault(image => image.IsPrimary)?.ImageUrl ??
          variant.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImageUrl ??
          variant.Product.Images.OrderBy(image => image.SortOrder).FirstOrDefault(image => image.IsPrimary)?.ImageUrl ??
          variant.Product.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImageUrl;

        var activePrice = variant.SalePrice ?? variant.Price;

        return new CartItemDto(
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
          activePrice * item.Quantity);
      })
      .ToList();

    return new CartDto(
      cart.Id,
      cart.UserId,
      items.Sum(item => item.Quantity),
      items.Sum(item => item.LineTotal),
      items);
  }
}
