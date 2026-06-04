namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatMessageDto(
  Guid Id,
  string Role,
  string Content,
  string? Intent,
  DateTime CreatedAt,
  IReadOnlyList<ChatAttachmentDto> Attachments,
  ChatStructuredPayloadDto? StructuredPayload);
