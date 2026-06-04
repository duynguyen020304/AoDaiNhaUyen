namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatAttachmentDto(
  Guid Id,
  string Kind,
  string FileUrl,
  string MimeType,
  string? OriginalFileName,
  DateTime CreatedAt);
