namespace AoDaiNhaUyen.Application.DTOs;

/// <summary>
/// DTO returned after changing product image visibility.
/// </summary>
public sealed record ProductImageVisibilityDto(
  Guid Id,
  bool IsPublic,
  string? PublicObjectKey,
  string ResolvedUrl);
