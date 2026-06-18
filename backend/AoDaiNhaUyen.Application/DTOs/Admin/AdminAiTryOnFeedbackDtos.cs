namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminAiTryOnFeedbackDto(
  Guid Id,
  Guid GeneratedImageId,
  string ImageUrl,
  Guid? UserId,
  string? UserName,
  string? UserEmail,
  int Rating,
  string? Comment,
  string? AdminNote,
  bool IsResolved,
  DateTime CreatedAt);

public sealed record UpdateAiTryOnFeedbackStatusDto(bool IsResolved, string? AdminNote);
