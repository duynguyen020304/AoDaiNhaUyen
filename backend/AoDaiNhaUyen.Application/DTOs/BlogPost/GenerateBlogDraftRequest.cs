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
  [MaxLength(20000)] public string? ExistingDraftJson { get; init; }
  [MaxLength(1000)] public string? RevisionInstruction { get; init; }
  [MaxLength(200)] public string? TargetSection { get; init; }
  public bool HasAskedClarification { get; init; }
}

public enum BlogGenerationPhase
{
  NeedsClarification,
  TemplateSelected,
  OutlineReady,
  Drafting,
  SeoRefining,
  ImagePrompting,
  ImageGenerating,
  Ready,
  Failed
}

public sealed record BlogGenerationPhaseStatus(
  BlogGenerationPhase Phase,
  string Label,
  string Status,
  string? Detail = null);

public sealed record BlogImagePlan(
  string FeaturedPrompt,
  string FeaturedAlt,
  string? FeaturedCaption,
  int InlineCount,
  int GalleryCount,
  IReadOnlyList<string> InlinePrompts,
  IReadOnlyList<string> GalleryPrompts);

public sealed record BlogGeneratedImagePreview(
  string Url,
  string? Alt = null,
  string? Label = null,
  string? Kind = null);

public sealed record BlogGenerationProgressResponse
{
  public required string Kind { get; init; }
  public required BlogGenerationPhase Phase { get; init; }
  public string? ConversationId { get; init; }
  public string? SelectedTemplate { get; init; }
  public string? TemplateReason { get; init; }
  public IReadOnlyList<string> Questions { get; init; } = [];
  public IReadOnlyList<string> SuggestedAnswers { get; init; } = [];
  public GeneratedBlogDraftResponse? Draft { get; init; }
  public BlogImagePlan? ImagePlan { get; init; }
  public IReadOnlyList<BlogGeneratedImagePreview> GeneratedImages { get; init; } = [];
  public BlogGenerationImageResult? ImageResult { get; init; }
  public IReadOnlyList<BlogGenerationPhaseStatus> Phases { get; init; } = [];
  public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>Structured AI-generated blog draft response.</summary>
public sealed record BlogImageAsset(
  string ObjectKey,
  string PublicUrl,
  string PreviewUrl,
  string AltText,
  string Label,
  string Kind,
  string Prompt,
  int? Width = null,
  int? Height = null,
  string? Caption = null);

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


public sealed record BlogGenerationImageResult(
  string Status,
  BlogImageAsset? FeaturedImage = null,
  IReadOnlyList<BlogImageAsset>? InlineImages = null,
  IReadOnlyList<BlogImageAsset>? GalleryImages = null,
  IReadOnlyList<string>? Warnings = null);
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
