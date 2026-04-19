namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatThreadSummaryDto(
  long Id,
  string Title,
  string? Preview,
  string Status,
  DateTime UpdatedAt);
