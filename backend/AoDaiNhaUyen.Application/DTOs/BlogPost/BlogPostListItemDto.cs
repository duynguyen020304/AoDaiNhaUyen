using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogPostListItemDto(
  Guid Id,
  string Title,
  string Slug,
  string Excerpt,
  string? FeaturedImage,
  int? FeaturedImageWidth,
  int? FeaturedImageHeight,
  BlogPostTemplate Template,
  IReadOnlyList<string> Tags,
  BlogCategoryDto? Category,
  string? AuthorName,
  BlogPostStatus Status,
  DateTime? PublishedAt,
  DateTime UpdatedAt);
