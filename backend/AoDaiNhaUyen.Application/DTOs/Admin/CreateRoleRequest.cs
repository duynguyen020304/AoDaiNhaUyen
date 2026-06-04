using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Request to create a new role.</summary>
public sealed record CreateRoleRequest
{
    [Required(ErrorMessage = "Tên vai trò là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Tên vai trò không được vượt quá 50 ký tự.")]
    public required string Name { get; init; }

    [StringLength(250, ErrorMessage = "Mô tả không được vượt quá 250 ký tự.")]
    public string? Description { get; init; }
}