namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatThreadSummaryDto(
  Guid Id,
  string Title,
  string? Preview,
  string Status,
  DateTime UpdatedAt);
