namespace AoDaiNhaUyen.Application.DTOs;

public sealed record IncomingChatAttachmentDto(
  string Kind,
  string OriginalFileName,
  string MimeType,
  byte[] Bytes);
