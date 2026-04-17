namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ProductDetailDto(
  long Id,
  string Name,
  string Slug,
  string ProductType,
  string Status,
  string? ShortDescription,
  string? Description,
  string? Material,
  string? Brand,
  string? Origin,
  string? CareInstruction,
  string CategoryName,
  string CategorySlug,
  bool IsFeatured,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  IReadOnlyList<ProductVariantDto> Variants,
  IReadOnlyList<ProductImageDto> Images);
