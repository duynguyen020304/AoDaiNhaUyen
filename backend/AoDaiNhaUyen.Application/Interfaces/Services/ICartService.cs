using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.DTOs.Cart;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICartService
{
  Task<AuthResult<CartDto>> GetCartAsync(long userId, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> AddItemAsync(long userId, AddCartItemDto request, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> UpdateItemAsync(long userId, long itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> RemoveItemAsync(long userId, long itemId, CancellationToken cancellationToken = default);
  Task<AuthResult<bool>> ClearCartAsync(long userId, CancellationToken cancellationToken = default);
}
