using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class BlogPost : BaseEntity
{
  public required string Title { get; set; }
  public required string Slug { get; set; }
  public required string Excerpt { get; set; }
  public string? FeaturedImage { get; set; }
  public int? FeaturedImageWidth { get; set; }
  public int? FeaturedImageHeight { get; set; }
  public BlogPostTemplate Template { get; set; } = BlogPostTemplate.StandardArticle;
  public required string Content { get; set; }
  public required string Tags { get; set; }
  public Guid? AuthorId { get; set; }
  public User? Author { get; set; }
  public string? AuthorNameOverride { get; set; }
  public string? AuthorBio { get; set; }
  public string? ReviewedBy { get; set; }
  public string? InformationGain { get; set; }
  public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;
  public DateTime? PublishedAt { get; set; }
  public string? MetaTitle { get; set; }
  public string? MetaDescription { get; set; }
  public string? CanonicalUrl { get; set; }
}
