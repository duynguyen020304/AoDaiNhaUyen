using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Marketing;

public sealed record SubscribeRequest
{
  [Required, EmailAddress, MaxLength(150)]
  public required string Email { get; init; }

  [MaxLength(80)]
  public string? Source { get; init; }
}

public sealed record TokenRequest
{
  [Required, MaxLength(128)]
  public required string Token { get; init; }
}

public sealed record SubscribeResultDto(string Email, string Status, string Message);
