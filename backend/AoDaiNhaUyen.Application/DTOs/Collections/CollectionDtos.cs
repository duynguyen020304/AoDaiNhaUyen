namespace AoDaiNhaUyen.Application.DTOs.Collections;

public sealed record CollectionListItemDto(
  Guid Id,
  string Name,
  string Slug,
  string? Description,
  string? CoverImageUrl,
  bool IsPublished,
  bool IsFeatured,
  int SortOrder,
  int ProductCount,
  DateTime? PublishedAt,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  bool IsDeleted);

public sealed record CollectionDetailDto(
  Guid Id,
  string Name,
  string Slug,
  string? Description,
  string? CoverImageUrl,
  bool IsPublished,
  bool IsFeatured,
  int SortOrder,
  DateTime? PublishedAt,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  bool IsDeleted,
  IReadOnlyList<CollectionProductDto> Products);

public sealed record CollectionProductDto(
  Guid Id,
  Guid ProductId,
  string ProductName,
  string ProductSlug,
  string? PrimaryImageUrl,
  int SortOrder);

public sealed record CreateCollectionRequest(
  string Name,
  string? Slug,
  string? Description,
  string? CoverImageUrl,
  bool IsPublished,
  bool IsFeatured,
  int SortOrder);

public sealed record UpdateCollectionRequest(
  string Name,
  string? Slug,
  string? Description,
  string? CoverImageUrl,
  bool IsPublished,
  bool IsFeatured,
  int SortOrder);

public sealed record AddProductToCollectionRequest(Guid ProductId, int SortOrder);
