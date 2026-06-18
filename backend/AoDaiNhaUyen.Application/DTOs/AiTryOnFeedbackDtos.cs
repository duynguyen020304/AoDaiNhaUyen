namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CreateAiTryOnFeedbackDto(
  Guid GeneratedImageId,
  int Rating,
  string? Comment);

public sealed record AiTryOnFeedbackDto(
  Guid Id,
  Guid GeneratedImageId,
  int Rating,
  string? Comment,
  DateTime CreatedAt);
