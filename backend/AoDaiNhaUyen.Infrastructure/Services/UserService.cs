using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class UserService(
    AppDbContext dbContext,
    IUserProfileRepository userProfileRepository,
    ILogger<UserService> logger) : IUserService
{
    public async Task<AuthResult<UserProfileDto>> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetUserProfileAsync(userId, cancellationToken);

        if (profile == null)
        {
            return AuthResult<UserProfileDto>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        return AuthResult<UserProfileDto>.Success(profile);
    }

    public async Task<AuthResult<UserProfileDto>> UpdateUserProfileAsync(long userId, UpdateUserProfileDto profile, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return AuthResult<UserProfileDto>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        user.FullName = string.IsNullOrWhiteSpace(profile.FullName) ? user.FullName : profile.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(profile.Phone) ? null : profile.Phone.Trim();
        user.Gender = string.IsNullOrWhiteSpace(profile.Gender) ? null : profile.Gender.Trim();
        user.DateOfBirth = profile.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("User {UserId} updated profile", userId);

            var updatedProfile = await userProfileRepository.GetUserProfileAsync(userId, cancellationToken);
            return AuthResult<UserProfileDto>.Success(updatedProfile!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update profile for user {UserId}", userId);
            return AuthResult<UserProfileDto>.Failure("update_failed", "Cập nhật hồ sơ thất bại.");
        }
    }

    public async Task<AuthResult<IReadOnlyList<UserAddressDto>>> GetUserAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return AuthResult<IReadOnlyList<UserAddressDto>>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        var addresses = await userProfileRepository.GetUserAddressesAsync(userId, cancellationToken);
        return AuthResult<IReadOnlyList<UserAddressDto>>.Success(addresses);
    }

    public async Task<AuthResult<UserAddressDto>> CreateUserAddressAsync(long userId, CreateAddressDto address, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return AuthResult<UserAddressDto>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        if (address.IsDefault)
        {
            var existingDefault = await dbContext.UserAddresses
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.IsDefault, cancellationToken);

            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
            }
        }

        var newAddress = new UserAddress
        {
            UserId = userId,
            RecipientName = address.RecipientName,
            RecipientPhone = address.RecipientPhone,
            Province = address.Province,
            District = address.District,
            Ward = address.Ward,
            AddressLine = address.AddressLine,
            IsDefault = address.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            dbContext.UserAddresses.Add(newAddress);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("User {UserId} created new address", userId);

            var addressDto = (await userProfileRepository.GetUserAddressesAsync(userId, cancellationToken))
                .FirstOrDefault(ua => ua.Id == newAddress.Id);

            return AuthResult<UserAddressDto>.Success(addressDto!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create address for user {UserId}", userId);
            return AuthResult<UserAddressDto>.Failure("address_creation_failed", "Tạo địa chỉ thất bại.");
        }
    }

    public async Task<AuthResult<bool>> DeleteUserAddressAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return AuthResult<bool>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        var address = await dbContext.UserAddresses
            .FirstOrDefaultAsync(ua => ua.Id == addressId && ua.UserId == userId, cancellationToken);

        if (address == null)
        {
            return AuthResult<bool>.Failure("address_not_found", "Địa chỉ không tồn tại.");
        }

        if (address.IsDefault)
        {
            return AuthResult<bool>.Failure("cannot_delete_default", "Không thể xóa địa chỉ mặc định.");
        }

        try
        {
            dbContext.UserAddresses.Remove(address);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("User {UserId} deleted address {AddressId}", userId, addressId);
            return AuthResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete address {AddressId} for user {UserId}", addressId, userId);
            return AuthResult<bool>.Failure("address_deletion_failed", "Xóa địa chỉ thất bại.");
        }
    }

    public async Task<AuthResult<PagedResult<UserOrderDto>>> GetUserOrdersAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return AuthResult<PagedResult<UserOrderDto>>.Failure("user_not_found", "Người dùng không tồn tại.");
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var orders = await userProfileRepository.GetUserOrdersAsync(userId, page, pageSize, cancellationToken);
        return AuthResult<PagedResult<UserOrderDto>>.Success(orders);
    }
}
