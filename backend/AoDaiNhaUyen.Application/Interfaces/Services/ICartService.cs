using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.DTOs.Cart;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICartService
{
  Task<AuthResult<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> AddItemAsync(Guid userId, AddCartItemDto request, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> UpdateItemAsync(Guid userId, Guid itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default);
  Task<AuthResult<CartDto>> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
  Task<AuthResult<bool>> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}
