namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ReviewDto(
  Guid Id,
  Guid UserId,
  string UserFullName,
  string? UserAvatarUrl,
  int Rating,
  string? Comment,
  DateTime CreatedAt);
