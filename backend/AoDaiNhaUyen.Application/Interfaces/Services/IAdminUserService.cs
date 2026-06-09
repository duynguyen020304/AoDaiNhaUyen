using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin user management service.</summary>
public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<AdminUserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUserListItemDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserListItemDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminMutationResult> UpdateUserRoleAsync(Guid actorUserId, Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<AdminMutationResult> UpdateUserStatusAsync(Guid actorUserId, Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete a user by setting IsDeleted flag.</summary>
    Task<AdminMutationResult> DeleteUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Restore a soft-deleted user.</summary>
    Task<AdminMutationResult> RestoreUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default);
}