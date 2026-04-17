namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnAccessoryImageDto(
  string Id,
  byte[] ImageBytes,
  string MimeType);
