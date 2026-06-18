using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Constants;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin user management service implementation.</summary>
public sealed class AdminUserService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHermesEventOutboxPublisher hermesEvents,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                u.Status,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt,
                u.LastLoginAt,
                u.IsDeleted))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<AdminUserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return null;

        return new AdminUserListItemDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Status,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAt,
            user.LastLoginAt,
            user.IsDeleted);
    }

    public async Task<AdminUserListItemDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new Domain.Entities.User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var account = new Domain.Entities.UserAccount
            {
                Provider = "credentials",
                ProviderAccountId = Guid.NewGuid().ToString(),
                PasswordHash = passwordHasher.HashPassword(request.Password),
                IsVerified = false
            };
            user.UserAccounts.Add(account);
        }

        if (request.RoleId.HasValue)
        {
            user.UserRoles.Add(new Domain.Entities.UserRole { RoleId = request.RoleId.Value });
        }
        else
        {
            var customerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Customer, cancellationToken);
            if (customerRole != null)
            {
                user.UserRoles.Add(new Domain.Entities.UserRole { RoleId = customerRole.Id });
            }
        }

        dbContext.Users.Add(user);
        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "admin_user_created",
            user.Id,
            new { targetUserId = user.Id, hasCredentialsAccount = !string.IsNullOrWhiteSpace(request.Password), roleId = request.RoleId, status = user.Status },
            $"admin_user_created:AdminSecurity:{user.Id:N}:{user.CreatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin created user {UserId} ({FullName})", user.Id, user.FullName);
        return (await GetUserByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<AdminUserListItemDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return null;

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "admin_user_updated",
            user.Id,
            new { targetUserId = user.Id, changedFields = new[] { "fullName", "email", "phone" } },
            $"admin_user_updated:AdminSecurity:{user.Id:N}:{user.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin updated user {UserId}", user.Id);
        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminMutationResult> UpdateUserRoleAsync(Guid actorUserId, Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return AdminMutationResult.Failure("not_found", "Không tìm thấy người dùng.");
        var newRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (newRole is null) return AdminMutationResult.Failure("invalid_role", "Vai trò không hợp lệ.");
        var oldRoles = string.Join(",", user.UserRoles.Select(ur => ur.Role.Name));
        var removesAdmin = user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin) && newRole.Name != RoleNames.Admin;
        if (id == actorUserId && removesAdmin) return AdminMutationResult.Failure("cannot_modify_self_role", "Không thể tự hạ quyền quản trị của chính mình.");
        if (removesAdmin && !await HasAnotherActiveAdminAsync(id, cancellationToken)) return AdminMutationResult.Failure("cannot_disable_last_admin", "Không thể hạ quyền quản trị viên cuối cùng.");
        user.UserRoles.Clear();
        user.UserRoles.Add(new Domain.Entities.UserRole { UserId = user.Id, RoleId = request.RoleId });
        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "role_permissions_changed",
            user.Id,
            new { targetUserId = user.Id, oldRoles, newRoleId = request.RoleId, actorUserId },
            $"role_permissions_changed:AdminSecurity:{user.Id:N}:{request.RoleId}:{user.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Admin {ActorUserId} changed role for user {UserId} from {OldRoles} to {RoleId}", actorUserId, user.Id, oldRoles, request.RoleId);
        return AdminMutationResult.Success();
    }

    public async Task<AdminMutationResult> UpdateUserStatusAsync(Guid actorUserId, Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return AdminMutationResult.Failure("not_found", "Không tìm thấy người dùng.");
        var disables = !string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase);
        if (id == actorUserId && disables) return AdminMutationResult.Failure("cannot_disable_self", "Không thể tự vô hiệu hóa tài khoản của chính mình.");
        if (disables && user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin) && !await HasAnotherActiveAdminAsync(id, cancellationToken)) return AdminMutationResult.Failure("cannot_disable_last_admin", "Không thể vô hiệu hóa quản trị viên cuối cùng.");
        var oldStatus = user.Status;
        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;
        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "admin_user_updated",
            user.Id,
            new { targetUserId = user.Id, oldStatus, newStatus = request.Status, actorUserId },
            $"admin_user_status_changed:AdminSecurity:{user.Id:N}:{request.Status}:{user.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Admin {ActorUserId} changed status for user {UserId} from {OldStatus} to {Status}", actorUserId, user.Id, oldStatus, request.Status);
        return AdminMutationResult.Success();
    }

    public async Task<AdminMutationResult> DeleteUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.IgnoreQueryFilters().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null || user.IsDeleted) return AdminMutationResult.Failure("not_found", "Người dùng không tồn tại hoặc đã bị xóa.");
        if (id == actorUserId) return AdminMutationResult.Failure("cannot_delete_self", "Không thể tự xóa tài khoản của chính mình.");
        if (user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin) && !await HasAnotherActiveAdminAsync(id, cancellationToken)) return AdminMutationResult.Failure("cannot_disable_last_admin", "Không thể xóa quản trị viên cuối cùng.");
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "admin_user_disabled",
            user.Id,
            new { targetUserId = user.Id, isDeleted = user.IsDeleted, actorUserId },
            $"admin_user_disabled:AdminSecurity:{user.Id:N}:{user.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Admin {ActorUserId} soft-deleted user {UserId}", actorUserId, user.Id);
        return AdminMutationResult.Success();
    }

    public async Task<AdminMutationResult> RestoreUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null || !user.IsDeleted) return AdminMutationResult.Failure("not_found", "Người dùng không tồn tại hoặc chưa bị xóa.");
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "admin_user_updated",
            user.Id,
            new { targetUserId = user.Id, action = "restored", actorUserId },
            $"admin_user_restored:AdminSecurity:{user.Id:N}:{user.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Admin {ActorUserId} restored user {UserId}", actorUserId, user.Id);
        return AdminMutationResult.Success();
    }

    private Task<bool> HasAnotherActiveAdminAsync(Guid excludedUserId, CancellationToken cancellationToken) =>
        dbContext.UserRoles.AnyAsync(ur => ur.UserId != excludedUserId && ur.Role.Name == RoleNames.Admin && !ur.User.IsDeleted && ur.User.Status == "active", cancellationToken);

    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

}