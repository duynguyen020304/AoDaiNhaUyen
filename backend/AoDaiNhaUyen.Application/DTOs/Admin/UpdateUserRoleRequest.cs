using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Request to update a user's role.</summary>
public sealed record UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Vai trò là bắt buộc.")]
    public required Guid RoleId { get; init; }
}