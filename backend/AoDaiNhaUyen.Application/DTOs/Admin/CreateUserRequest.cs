using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Request to create a new user.</summary>
public sealed record CreateUserRequest
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public required string FullName { get; init; }

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string? Email { get; init; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; init; }

    [StringLength(50, ErrorMessage = "Mật khẩu không được vượt quá 50 ký tự.")]
    public string? Password { get; init; }

    public Guid? RoleId { get; init; }
}