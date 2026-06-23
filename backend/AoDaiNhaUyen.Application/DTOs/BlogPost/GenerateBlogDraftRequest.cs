using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

/// <summary>Request for generating an AI-assisted blog draft.</summary>
public sealed record GenerateBlogDraftRequest
{
  [Required, MaxLength(500)] public required string Topic { get; init; }
  [MaxLength(200)] public string? TargetKeyword { get; init; }
  [MaxLength(200)] public string? Audience { get; init; }
  [MaxLength(100)] public string? Tone { get; init; }
  public BlogPostTemplate Template { get; init; } = BlogPostTemplate.StandardArticle;
  public Guid? CategoryId { get; init; }
  public IReadOnlyList<string> ProductSlugs { get; init; } = [];
  [MaxLength(20)] public string? Length { get; init; }
  public bool IncludeFaq { get; init; } = true;
  [MaxLength(2000)] public string? Notes { get; init; }
}

/// <summary>Structured AI-generated blog draft response.</summary>
public sealed record GeneratedBlogDraftResponse(
  string Title,
  string Slug,
  string Excerpt,
  BlogPostTemplate Template,
  JsonElement Content,
  IReadOnlyList<string> Tags,
  string? MetaTitle,
  string? MetaDescription,
  string? CanonicalUrl,
  string? InformationGain,
  string? AuthorNameOverride,
  string? AuthorBio,
  string? ReviewedBy,
  Guid? BlogCategoryId,
  IReadOnlyList<string> QualityWarnings,
  IReadOnlyList<string>? Outline = null,
  string? ImagePrompt = null,
  BlogTryOnHandoffDto? TryOnHandoff = null,
  BlogDraftValidationDto? Validation = null);

public sealed record BlogTryOnHandoffDto(
  string FrontendUrl,
  string ApiEndpoint,
  string Status,
  IReadOnlyList<string> RequiredInputs,
  string? ProductId = null,
  string? ProductName = null,
  string? Note = null);

public sealed record BlogDraftValidationDto(
  bool Passed,
  IReadOnlyList<string> Warnings,
  IReadOnlyList<string> Checks);
