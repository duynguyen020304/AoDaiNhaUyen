using AoDaiNhaUyen.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record CreateBlogPostRequest
{
  [Required, MaxLength(500)] public required string Title { get; init; }
  [MaxLength(500)] public string? Slug { get; init; }
  [Required] public required string Excerpt { get; init; }
  [MaxLength(1000)] public string? FeaturedImage { get; init; }
  public int? FeaturedImageWidth { get; init; }
  public int? FeaturedImageHeight { get; init; }
  public BlogPostTemplate Template { get; init; } = BlogPostTemplate.StandardArticle;
  public required JsonElement Content { get; init; }
  public IReadOnlyList<string> Tags { get; init; } = [];
  public Guid? BlogCategoryId { get; init; }
  public Guid? AuthorId { get; init; }
  [MaxLength(200)] public string? AuthorNameOverride { get; init; }
  public string? AuthorBio { get; init; }
  [MaxLength(200)] public string? ReviewedBy { get; init; }
  public string? InformationGain { get; init; }
  public BlogPostStatus Status { get; init; } = BlogPostStatus.Draft;
  public DateTime? PublishedAt { get; init; }
  [MaxLength(200)] public string? MetaTitle { get; init; }
  [MaxLength(500)] public string? MetaDescription { get; init; }
  [MaxLength(2000)] public string? CanonicalUrl { get; init; }
}
