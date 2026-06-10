using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogPostDto(
  Guid Id,
  string Title,
  string Slug,
  string Excerpt,
  string? FeaturedImage,
  int? FeaturedImageWidth,
  int? FeaturedImageHeight,
  BlogPostTemplate Template,
  IReadOnlyList<BlogBlockDto> Content,
  IReadOnlyList<string> Tags,
  BlogCategoryDto? Category,
  Guid? BlogCategoryId,
  Guid? AuthorId,
  string? AuthorName,
  string? AuthorAvatarUrl,
  string? AuthorBio,
  string? ReviewedBy,
  string? InformationGain,
  BlogPostStatus Status,
  DateTime? PublishedAt,
  string? MetaTitle,
  string? MetaDescription,
  string? CanonicalUrl,
  DateTime CreatedAt,
  DateTime UpdatedAt);
