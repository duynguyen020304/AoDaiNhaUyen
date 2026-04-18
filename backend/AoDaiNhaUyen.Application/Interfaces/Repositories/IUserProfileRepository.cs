using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfileDto?> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAddressDto>> GetUserAddressesAsync(long userId, CancellationToken cancellationToken = default);
    Task<PagedResult<UserOrderDto>> GetUserOrdersAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default);
}