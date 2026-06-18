using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin role management service implementation.</summary>
public sealed class AdminRoleService(
    AppDbContext dbContext,
    IHermesEventOutboxPublisher hermesEvents) : IAdminRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new Domain.Entities.Role
        {
            Name = request.Name,
            Description = request.Description
        };

        dbContext.Roles.Add(role);
        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "role_created",
            role.Id,
            new { roleId = role.Id, roleName = role.Name },
            $"role_created:AdminSecurity:{role.Id:N}:{role.CreatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RoleDto(role.Id, role.Name, role.Description);
    }

    public async Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return null;

        role.Name = request.Name;
        role.Description = request.Description;

        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "role_updated",
            role.Id,
            new { roleId = role.Id, roleName = role.Name },
            $"role_updated:AdminSecurity:{role.Id:N}:{role.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RoleDto(role.Id, role.Name, role.Description);
    }

    public async Task<bool> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role is null) return false;

        // Prevent deleting role if users are assigned to it
        if (role.UserRoles.Any()) return false;

        dbContext.Roles.Remove(role);

        await hermesEvents.EnqueueAdminSecurityEventAsync(
            "role_deleted",
            role.Id,
            new { roleId = role.Id, roleName = role.Name },
            $"role_deleted:AdminSecurity:{role.Id:N}:{DateTime.UtcNow.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}