using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Request to update an existing user.</summary>
public sealed record UpdateUserRequest
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public required string FullName { get; init; }

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string? Email { get; init; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; init; }
}