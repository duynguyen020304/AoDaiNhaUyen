using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface ICartRepository
{
  Task<Cart?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
  Task<Cart?> GetByUserIdWithItemsAsync(long userId, CancellationToken cancellationToken = default);
  Task<CartItem?> GetItemByIdAsync(long userId, long itemId, CancellationToken cancellationToken = default);
  Task<ProductVariant?> GetVariantForCartAsync(long variantId, CancellationToken cancellationToken = default);
  Task<Cart> CreateForUserAsync(long userId, CancellationToken cancellationToken = default);
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
  CartDto MapCart(Cart cart);
}
