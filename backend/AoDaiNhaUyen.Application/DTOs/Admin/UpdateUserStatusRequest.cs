using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Request to update a user's status.</summary>
public sealed record UpdateUserStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    [RegularExpression("^(active|inactive|blocked)$", ErrorMessage = "Trạng thái không hợp lệ. Cho phép: active, inactive, blocked.")]
    public required string Status { get; init; }
}