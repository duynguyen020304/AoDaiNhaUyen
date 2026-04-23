using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.DTOs.Auth;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IUserService
{
    Task<AuthResult<UserProfileDto>> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<AuthResult<UserProfileDto>> UpdateUserProfileAsync(long userId, UpdateUserProfileDto profile, CancellationToken cancellationToken = default);
    Task<AuthResult<IReadOnlyList<UserAddressDto>>> GetUserAddressesAsync(long userId, CancellationToken cancellationToken = default);
    Task<AuthResult<UserAddressDto>> CreateUserAddressAsync(long userId, CreateAddressDto address, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> DeleteUserAddressAsync(long userId, long addressId, CancellationToken cancellationToken = default);
    Task<AuthResult<PagedResult<UserOrderDto>>> GetUserOrdersAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
