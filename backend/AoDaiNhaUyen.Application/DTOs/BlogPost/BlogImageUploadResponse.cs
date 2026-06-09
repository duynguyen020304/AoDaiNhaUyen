namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogImageUploadResponse(Guid ImageId, string Url, string ObjectKey, int? Width, int? Height);
