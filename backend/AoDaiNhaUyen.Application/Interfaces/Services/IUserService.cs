using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IUserService
{
    Task<AuthResult<UserProfileDto>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResult<UserProfileDto>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto profile, CancellationToken cancellationToken = default);
    Task<AuthResult<IReadOnlyList<UserAddressDto>>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResult<UserAddressDto>> CreateUserAddressAsync(Guid userId, CreateAddressDto address, CancellationToken cancellationToken = default);
    Task<AuthResult<UserAddressDto>> UpdateUserAddressAsync(Guid userId, Guid addressId, CreateAddressDto address, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> DeleteUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
    Task<AuthResult<PagedResult<UserOrderDto>>> GetUserOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
