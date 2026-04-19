namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatMessageDto(
  long Id,
  string Role,
  string Content,
  string? Intent,
  DateTime CreatedAt,
  IReadOnlyList<ChatAttachmentDto> Attachments,
  ChatStructuredPayloadDto? StructuredPayload);
