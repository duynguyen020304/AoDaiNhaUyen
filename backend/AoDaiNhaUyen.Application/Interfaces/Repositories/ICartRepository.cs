using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface ICartRepository
{
  Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
  Task<Cart?> GetByUserIdWithItemsAsync(Guid userId, CancellationToken cancellationToken = default);
  Task<CartItem?> GetItemByIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
  Task<ProductVariant?> GetVariantForCartAsync(Guid variantId, CancellationToken cancellationToken = default);
  Task<Cart> CreateForUserAsync(Guid userId, CancellationToken cancellationToken = default);
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
  Task<CartDto> MapCartAsync(Cart cart, CancellationToken cancellationToken = default);
}
