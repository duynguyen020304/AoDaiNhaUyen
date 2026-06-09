namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogImageVisibilityDto(Guid ImageId, bool IsPublic, string? PublicObjectKey, string Url);
