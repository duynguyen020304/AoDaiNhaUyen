namespace AoDaiNhaUyen.Application.DTOs;

public sealed record AiTryOnAccessoryImageDto(
  string Id,
  string DisplayName,
  byte[] ImageBytes,
  string MimeType);
