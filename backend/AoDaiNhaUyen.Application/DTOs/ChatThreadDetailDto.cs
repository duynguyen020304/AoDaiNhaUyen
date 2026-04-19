namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatThreadDetailDto(
  long Id,
  string Title,
  string Status,
  string Source,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  IReadOnlyList<ChatMessageDto> Messages);
