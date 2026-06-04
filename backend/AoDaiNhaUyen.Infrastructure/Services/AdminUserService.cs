using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Constants;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin user management service implementation.</summary>
public sealed class AdminUserService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher) : IAdminUserService
{
    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.Status != "deleted")
            .AsQueryable();

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
                u.LastLoginAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<AdminUserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", cancellationToken);

        if (user is null) return null;

        return new AdminUserListItemDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Status,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAt,
            user.LastLoginAt);
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
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetUserByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<AdminUserListItemDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", cancellationToken);

        if (user is null) return null;

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<bool> UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", cancellationToken);

        if (user is null) return false;

        var roleExists = await dbContext.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists) return false;

        user.UserRoles.Clear();
        user.UserRoles.Add(new Domain.Entities.UserRole { UserId = user.Id, RoleId = request.RoleId });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", cancellationToken);

        if (user is null) return false;

        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", cancellationToken);

        if (user is null) return false;

        user.Status = "deleted";
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}