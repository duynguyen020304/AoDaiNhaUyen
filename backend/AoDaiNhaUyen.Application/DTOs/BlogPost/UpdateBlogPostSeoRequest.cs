using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record UpdateBlogPostSeoRequest
{
  [MaxLength(200)] public string? MetaTitle { get; init; }
  [MaxLength(500)] public string? MetaDescription { get; init; }
  [MaxLength(2000)] public string? CanonicalUrl { get; init; }
  [MaxLength(200)] public string? ReviewedBy { get; init; }
  public string? InformationGain { get; init; }
  public IReadOnlyList<string>? Tags { get; init; }
}
