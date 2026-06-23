namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminGeneratedImageDto(
  byte[] Bytes,
  string MimeType,
  string Prompt);
