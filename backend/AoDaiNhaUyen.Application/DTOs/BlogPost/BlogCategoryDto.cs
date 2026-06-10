namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogCategoryDto(
  Guid Id,
  string Name,
  string Slug,
  string? Description,
  int SortOrder,
  int PublishedPostCount);
