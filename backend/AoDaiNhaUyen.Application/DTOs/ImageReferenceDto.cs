namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ImageReferenceDto(
    long AttachmentId,
    string Label,
    string Kind,
    string MimeType,
    byte[] Bytes);
