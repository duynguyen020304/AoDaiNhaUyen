namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatAttachmentDto(
  long Id,
  string Kind,
  string FileUrl,
  string MimeType,
  string? OriginalFileName,
  DateTime CreatedAt);
