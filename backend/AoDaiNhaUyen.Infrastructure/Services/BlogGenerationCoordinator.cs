using System.Text.Json;
using System.Linq;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class BlogGenerationCoordinator(
  IBlogAiDraftService blogAiDrafts,
  IAdminBlogImageGenerationService blogImageGeneration,
  IStorageService storageService,
  AppDbContext db,
  Microsoft.Extensions.Logging.ILogger<BlogGenerationCoordinator> logger) : IBlogGenerationCoordinator
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private const int MinWordCount = 1500;
  private const int MaxExpansionIterations = 2;

  public async Task<BlogGenerationProgressResponse> GenerateAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken = default)
  {
    var enriched = EnrichRequest(request);
    var template = SelectTemplate(enriched);
    enriched = enriched with { Template = template.Template };

    if (!HasRevisionRequest(enriched) && NeedsClarification(enriched))
    {
      if (enriched.HasAskedClarification)
      {
        enriched = ApplyFallbackDefaults(enriched);
      }
      else
      {
        return new BlogGenerationProgressResponse
        {
          Kind = "blog_draft_clarification",
          Phase = BlogGenerationPhase.NeedsClarification,
          SelectedTemplate = template.Template.ToString(),
          TemplateReason = template.Reason,
          Questions = BuildClarificationQuestions(enriched),
          SuggestedAnswers = BuildSuggestedAnswers(template.Template),
          Phases = BuildPhases(BlogGenerationPhase.NeedsClarification),
          Warnings = ["Cần thêm một ít thông tin để bài đủ sâu và đúng mục tiêu tìm kiếm."]
        };
      }
    }

    var draft = await CreateOrReviseDraftAsync(enriched, cancellationToken);
    draft = await EnsureCompleteDraftAsync(enriched, draft, cancellationToken);

    var imagePlan = BuildImagePlan(enriched, draft, template.Template);
    var imageResult = await TryGenerateImagesAsync(enriched, imagePlan, cancellationToken);
    var mergedDraft = MergeImagesIntoDraft(draft, imageResult);

    return new BlogGenerationProgressResponse
    {
      Kind = "blog_draft",
      Phase = BlogGenerationPhase.Ready,
      SelectedTemplate = template.Template.ToString(),
      TemplateReason = template.Reason,
      Draft = mergedDraft,
      ImagePlan = imagePlan,
      GeneratedImages = BuildGeneratedPreviews(imageResult),
      ImageResult = imageResult,
      Phases = BuildPhases(BlogGenerationPhase.Ready, imageResult?.Status == "skipped"),
      Warnings = MergeWarnings(mergedDraft, imageResult)
    };
  }

  private async Task<GeneratedBlogDraftResponse> CreateOrReviseDraftAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken)
  {
    if (HasRevisionRequest(request) && TryParseExistingDraft(request, out var existingDraft))
    {
      return await blogAiDrafts.ExpandDraftAsync(
        request,
        existingDraft,
        BuildRevisionGoal(request),
        cancellationToken);
    }

    return await blogAiDrafts.GenerateDraftAsync(request, cancellationToken);
  }

  private static bool HasRevisionRequest(GenerateBlogDraftRequest request) =>
    !string.IsNullOrWhiteSpace(request.RevisionInstruction)
    || !string.IsNullOrWhiteSpace(request.TargetSection)
    || !string.IsNullOrWhiteSpace(request.ExistingDraftJson);

  private static string BuildRevisionGoal(GenerateBlogDraftRequest request)
  {
    var target = string.IsNullOrWhiteSpace(request.TargetSection) ? "toàn bài" : request.TargetSection;
    var instruction = string.IsNullOrWhiteSpace(request.RevisionInstruction)
      ? "Điều chỉnh theo phản hồi mới của admin, giữ phần còn lại ổn định."
      : request.RevisionInstruction;
    return $"Chỉnh đúng phần admin yêu cầu: {target}. Yêu cầu chỉnh sửa: {instruction}. Giữ tối đa phần tốt sẵn có, chỉ sửa đúng vùng liên quan và các phần phụ thuộc cần thiết.";
  }

  private static bool TryParseExistingDraft(GenerateBlogDraftRequest request, out GeneratedBlogDraftResponse draft)
  {
    draft = default!;
    if (string.IsNullOrWhiteSpace(request.ExistingDraftJson)) return false;

    try
    {
      using var doc = JsonDocument.Parse(request.ExistingDraftJson);
      var root = doc.RootElement;
      if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("draft", out var nestedDraft))
        root = nestedDraft;

      if (!root.TryGetProperty("title", out var titleEl) || !root.TryGetProperty("content", out var contentEl))
        return false;

      var title = titleEl.GetString() ?? request.Topic;
      var excerpt = root.TryGetProperty("excerpt", out var excerptEl) ? excerptEl.GetString() ?? title : title;
      var slug = root.TryGetProperty("slug", out var slugEl) ? slugEl.GetString() ?? title : title;
      var template = request.Template;
      if (root.TryGetProperty("template", out var templateEl) && Enum.TryParse<BlogPostTemplate>(templateEl.GetString(), true, out var parsedTemplate))
        template = parsedTemplate;

      var tags = root.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array
        ? tagsEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        : new List<string>();
      var warnings = root.TryGetProperty("qualityWarnings", out var warningsEl) && warningsEl.ValueKind == JsonValueKind.Array
        ? warningsEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        : new List<string>();
      var outline = root.TryGetProperty("outline", out var outlineEl) && outlineEl.ValueKind == JsonValueKind.Array
        ? outlineEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        : null;

      draft = new GeneratedBlogDraftResponse(
        title,
        slug,
        excerpt,
        template,
        contentEl.Clone(),
        tags,
        root.TryGetProperty("metaTitle", out var metaTitleEl) ? metaTitleEl.GetString() : null,
        root.TryGetProperty("metaDescription", out var metaDescriptionEl) ? metaDescriptionEl.GetString() : null,
        root.TryGetProperty("canonicalUrl", out var canonicalEl) ? canonicalEl.GetString() : null,
        root.TryGetProperty("informationGain", out var infoGainEl) ? infoGainEl.GetString() : null,
        root.TryGetProperty("authorNameOverride", out var authorNameEl) ? authorNameEl.GetString() : null,
        root.TryGetProperty("authorBio", out var authorBioEl) ? authorBioEl.GetString() : null,
        root.TryGetProperty("reviewedBy", out var reviewedByEl) ? reviewedByEl.GetString() : null,
        null,
        warnings,
        outline,
        root.TryGetProperty("imagePrompt", out var imagePromptEl) ? imagePromptEl.GetString() : null,
        null,
        null);
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static GenerateBlogDraftRequest EnrichRequest(GenerateBlogDraftRequest request)
  {
    var topic = request.Topic.Trim();
    var inferredKeyword = string.IsNullOrWhiteSpace(request.TargetKeyword) ? topic : request.TargetKeyword;
    var inferredAudience = string.IsNullOrWhiteSpace(request.Audience) ? "Khách hàng đang tìm hiểu áo dài cao cấp" : request.Audience;
    var inferredTone = string.IsNullOrWhiteSpace(request.Tone) ? "Trang nhã, tư vấn chuyên sâu" : request.Tone;
    var inferredLength = request.Length is "short" or "standard" or "long" ? request.Length : "long";
    return request with
    {
      Topic = topic,
      TargetKeyword = inferredKeyword,
      Audience = inferredAudience,
      Tone = inferredTone,
      Length = inferredLength,
      IncludeFaq = true
    };
  }

  private static GenerateBlogDraftRequest ApplyFallbackDefaults(GenerateBlogDraftRequest request) => request with
  {
    TargetKeyword = request.TargetKeyword ?? request.Topic,
    Audience = request.Audience ?? "Người đang cân nhắc mua hoặc may áo dài",
    Tone = request.Tone ?? "Tư vấn premium, dễ hiểu",
    Length = request.Length ?? "long"
  };

  private static bool NeedsClarification(GenerateBlogDraftRequest request)
  {
    var shortTopic = request.Topic.Trim().Length < 18 || request.Topic.Trim().Equals("hãy tạo bài blog post thử đi", StringComparison.OrdinalIgnoreCase);
    var lacksKeyword = string.IsNullOrWhiteSpace(request.TargetKeyword);
    var spotlightWithoutProducts = request.Template == BlogPostTemplate.ProductSpotlight && request.ProductSlugs.Count == 0;
    return shortTopic || lacksKeyword || spotlightWithoutProducts;
  }

  private static IReadOnlyList<string> BuildClarificationQuestions(GenerateBlogDraftRequest request) =>
  [
    "Bạn muốn bài tập trung vào chủ đề/từ khóa nào?",
    "Bài này nhắm tới cô dâu, khách dự tiệc, người cần may đo hay người mới tìm hiểu áo dài?",
    "Bạn muốn dạng bài hướng dẫn, lookbook ảnh, spotlight sản phẩm hay bài tư vấn chuẩn?"
  ];

  private static IReadOnlyList<string> BuildSuggestedAnswers(BlogPostTemplate template) =>
  [
    "Giữ mặc định SEO dài 1500+ từ",
    template switch
    {
      BlogPostTemplate.HowTo => "Dạng hướng dẫn từng bước",
      BlogPostTemplate.PhotoGallery => "Dạng lookbook/thư viện ảnh",
      BlogPostTemplate.ProductSpotlight => "Dạng giới thiệu sản phẩm",
      _ => "Dạng bài tư vấn chuẩn"
    },
    "Nhắm tới khách hàng đang cân nhắc chọn áo dài"
  ];

  private static (BlogPostTemplate Template, string Reason) SelectTemplate(GenerateBlogDraftRequest request)
  {
    var text = $"{request.Topic} {request.TargetKeyword} {request.Notes}".ToLowerInvariant();
    if (request.Template != BlogPostTemplate.StandardArticle)
      return (request.Template, "Ưu tiên template admin đã gợi ý.");
    if (text.Contains("cách") || text.Contains("hướng dẫn") || text.Contains("bước"))
      return (BlogPostTemplate.HowTo, "Nội dung mang ý định hướng dẫn theo từng bước.");
    if (text.Contains("lookbook") || text.Contains("bộ sưu tập") || text.Contains("mẫu") || text.Contains("gallery"))
      return (BlogPostTemplate.PhotoGallery, "Chủ đề thiên về trình bày hình ảnh/bộ sưu tập.");
    if (text.Contains("video") || text.Contains("clip"))
      return (BlogPostTemplate.VideoFeature, "Chủ đề có dấu hiệu ưu tiên video.");
    if (request.ProductSlugs.Count > 0 || text.Contains("sản phẩm") || text.Contains("giá"))
      return (BlogPostTemplate.ProductSpotlight, "Chủ đề thiên về giới thiệu sản phẩm cụ thể.");
    return (BlogPostTemplate.StandardArticle, "Phù hợp bài tư vấn SEO chuẩn, dễ mở rộng chiều sâu nội dung.");
  }

  private async Task<GeneratedBlogDraftResponse> EnsureCompleteDraftAsync(
    GenerateBlogDraftRequest request,
    GeneratedBlogDraftResponse draft,
    CancellationToken cancellationToken)
  {
    var current = draft;
    for (var i = 0; i < MaxExpansionIterations; i++)
    {
      if (PassesQualityGate(request, current)) return current;
      current = await blogAiDrafts.ExpandDraftAsync(request, current, "Bổ sung section còn thiếu, tăng chiều sâu SEO, hoàn thiện đủ block và độ dài.", cancellationToken);
    }
    return current;
  }

  private static bool PassesQualityGate(GenerateBlogDraftRequest request, GeneratedBlogDraftResponse draft)
  {
    var words = CountWords(draft.Content);
    var longEnough = request.Length == "short" ? words >= 600 : words >= MinWordCount;
    var hasMeta = !string.IsNullOrWhiteSpace(draft.MetaTitle) && !string.IsNullOrWhiteSpace(draft.MetaDescription);
    var hasTags = draft.Tags.Count >= 3;
    var hasOutline = draft.Outline?.Count >= 3;
    var contentText = draft.Content.GetRawText();
    var hasSteps = request.Template != BlogPostTemplate.HowTo || contentText.Contains("\"type\":\"step\"", StringComparison.OrdinalIgnoreCase);
    return longEnough && hasMeta && hasTags && hasOutline && hasSteps;
  }

  private static int CountWords(JsonElement content)
  {
    if (content.ValueKind != JsonValueKind.Array) return 0;
    var total = 0;
    foreach (var block in content.EnumerateArray())
    {
      if (block.ValueKind != JsonValueKind.Object) continue;
      if (block.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
        total += contentProp.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
      if (block.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
        total += titleProp.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
    }
    return total;
  }

  private static BlogImagePlan BuildImagePlan(GenerateBlogDraftRequest request, GeneratedBlogDraftResponse draft, BlogPostTemplate template)
  {
    var featuredPrompt = $"{draft.ImagePrompt ?? draft.Title}. 1.91:1, 1200x630, premium Vietnamese áo dài editorial, no text overlay.";
    var featuredAlt = $"Ảnh nổi bật cho bài viết {draft.Title}";
    var inlineCount = template == BlogPostTemplate.HowTo ? 1 : 0;
    var galleryCount = template == BlogPostTemplate.PhotoGallery ? 3 : 0;
    return new BlogImagePlan(
      featuredPrompt,
      featuredAlt,
      draft.Title,
      inlineCount,
      galleryCount,
      inlineCount > 0 ? [ $"Ảnh minh họa theo nội dung bài {draft.Title}, khung 3:2, chi tiết hữu ích cho section chính." ] : [],
      galleryCount > 0 ? Enumerable.Range(1, galleryCount).Select(i => $"Lookbook ảnh {i} cho {draft.Title}, bố cục 4:3, cùng palette cao cấp.").ToList() : []);
  }

  private async Task<BlogGenerationImageResult> TryGenerateImagesAsync(
    GenerateBlogDraftRequest request,
    BlogImagePlan plan,
    CancellationToken cancellationToken)
  {
    try
    {
      var featured = await GenerateImageAssetAsync(request.Topic, plan.FeaturedPrompt, plan.FeaturedAlt, "Ảnh nổi bật", "featured", 1200, 630, cancellationToken);
      var inlineImages = new List<BlogImageAsset>();
      for (var i = 0; i < plan.InlinePrompts.Count; i++)
        inlineImages.Add(await GenerateImageAssetAsync(request.Topic, plan.InlinePrompts[i], $"Ảnh minh họa {i + 1} cho bài {request.Topic}", $"Ảnh đơn lẻ {i + 1}", $"inline-{i + 1}", 1200, 800, cancellationToken));
      var galleryImages = new List<BlogImageAsset>();
      for (var i = 0; i < plan.GalleryPrompts.Count; i++)
        galleryImages.Add(await GenerateImageAssetAsync(request.Topic, plan.GalleryPrompts[i], $"Ảnh gallery {i + 1} cho bài {request.Topic}", $"Gallery {i + 1}", $"gallery-{i + 1}", 800, 600, cancellationToken));
      return new BlogGenerationImageResult("generated", featured, inlineImages, galleryImages, []);
    }
    catch (Exception ex) when (IsQuotaOrImageFailure(ex))
    {
      logger.LogWarning(ex, "[BlogGen] Image generation skipped.");
      return new BlogGenerationImageResult("skipped", null, [], [], ["Không tạo được ảnh trong lượt này; vẫn có thể mở và chỉnh sửa bài viết."]);
    }
  }

  private async Task<BlogImageAsset> GenerateImageAssetAsync(string topic, string prompt, string altText, string label, string kind, int width, int height, CancellationToken cancellationToken)
  {
    var safeAlt = (altText.Length > 125 ? altText[..125] : altText).Trim();
    var image = await blogImageGeneration.GenerateAsync(prompt, cancellationToken);
    var extension = image.MimeType.ToLowerInvariant() switch { "image/jpeg" => ".jpg", "image/webp" => ".webp", _ => ".png" };
    var fileName = $"blog-ai-{kind}-{Guid.NewGuid():N}{extension}";
    await using var stream = new MemoryStream(image.Bytes);
    var upload = await storageService.UploadAsync(stream, fileName, image.MimeType, "private/blog", cancellationToken);
    try
    {
      var publicUrl = await storageService.CopyToPublicBlogAsync(upload.ObjectKey, cancellationToken);
      db.BlogImages.Add(new BlogImage { ImageUrl = upload.ObjectKey, AltText = safeAlt, IsPublic = true, PublicObjectKey = $"aodainhauyen/public/blog/{upload.ObjectKey.Split('/').Last()}", SortOrder = 0 });
      await db.SaveChangesAsync(cancellationToken);
      return new BlogImageAsset(upload.ObjectKey, publicUrl, publicUrl, safeAlt, label, kind, prompt, width, height, label);
    }
    catch
    {
      await storageService.DeleteAsync(upload.ObjectKey, cancellationToken);
      throw;
    }
  }

  private static bool IsQuotaOrImageFailure(Exception ex) => ex is AiTryOnProviderException || ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("storage", StringComparison.OrdinalIgnoreCase);

  private static GeneratedBlogDraftResponse MergeImagesIntoDraft(GeneratedBlogDraftResponse draft, BlogGenerationImageResult imageResult)
  {
    if (imageResult.FeaturedImage is null && (imageResult.InlineImages?.Count ?? 0) == 0 && (imageResult.GalleryImages?.Count ?? 0) == 0)
      return draft;

    var blocks = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(draft.Content.GetRawText(), JsonOptions) ?? [];
    if (imageResult.InlineImages is not null)
    {
      foreach (var image in imageResult.InlineImages)
      {
        blocks.Add(new Dictionary<string, object?>
        {
          ["type"] = "image",
          ["src"] = image.ObjectKey,
          ["alt"] = image.AltText,
          ["caption"] = image.Caption,
          ["width"] = "contained",
          ["widthPx"] = image.Width,
          ["heightPx"] = image.Height
        });
      }
    }
    if (imageResult.GalleryImages is not null && imageResult.GalleryImages.Count > 0)
    {
      blocks.Add(new Dictionary<string, object?>
      {
        ["type"] = "gallery",
        ["images"] = imageResult.GalleryImages.Select(image => new Dictionary<string, object?>
        {
          ["src"] = image.ObjectKey,
          ["alt"] = image.AltText,
          ["caption"] = image.Caption,
          ["widthPx"] = image.Width,
          ["heightPx"] = image.Height
        }).ToList()
      });
    }

    var warnings = draft.QualityWarnings.ToList();
    if (imageResult.Warnings is not null) warnings.AddRange(imageResult.Warnings);

    return draft with
    {
      Content = JsonSerializer.SerializeToElement(blocks, JsonOptions),
      QualityWarnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    };
  }

  private static IReadOnlyList<BlogGeneratedImagePreview> BuildGeneratedPreviews(BlogGenerationImageResult? imageResult)
  {
    var list = new List<BlogGeneratedImagePreview>();
    if (imageResult?.FeaturedImage is not null)
      list.Add(new BlogGeneratedImagePreview(imageResult.FeaturedImage.PublicUrl, imageResult.FeaturedImage.AltText, imageResult.FeaturedImage.Label, imageResult.FeaturedImage.Kind));
    if (imageResult?.InlineImages is not null)
      list.AddRange(imageResult.InlineImages.Select(x => new BlogGeneratedImagePreview(x.PublicUrl, x.AltText, x.Label, x.Kind)));
    if (imageResult?.GalleryImages is not null)
      list.AddRange(imageResult.GalleryImages.Select(x => new BlogGeneratedImagePreview(x.PublicUrl, x.AltText, x.Label, x.Kind)));
    return list;
  }

  private static IReadOnlyList<string> MergeWarnings(GeneratedBlogDraftResponse draft, BlogGenerationImageResult? imageResult)
  {
    var warnings = draft.QualityWarnings.ToList();
    if (imageResult?.Warnings is not null) warnings.AddRange(imageResult.Warnings);
    return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  }

  private static IReadOnlyList<BlogGenerationPhaseStatus> BuildPhases(BlogGenerationPhase finalPhase, bool imageSkipped = false)
  {
    return
    [
      new(BlogGenerationPhase.TemplateSelected, "Intake checked", "completed"),
      new(BlogGenerationPhase.TemplateSelected, "Template selected", "completed"),
      new(BlogGenerationPhase.OutlineReady, "Outline built", finalPhase >= BlogGenerationPhase.OutlineReady ? "completed" : "pending"),
      new(BlogGenerationPhase.Drafting, "Draft expanded", finalPhase >= BlogGenerationPhase.Drafting ? "completed" : "pending"),
      new(BlogGenerationPhase.Drafting, "Continuation loops completed", finalPhase >= BlogGenerationPhase.Drafting ? "completed" : "pending"),
      new(BlogGenerationPhase.SeoRefining, "SEO refined", finalPhase >= BlogGenerationPhase.SeoRefining ? "completed" : "pending"),
      new(BlogGenerationPhase.ImagePrompting, "Image prompts prepared", finalPhase >= BlogGenerationPhase.ImagePrompting ? "completed" : "pending"),
      new(BlogGenerationPhase.ImageGenerating, imageSkipped ? "Images skipped" : "Images generated", finalPhase >= BlogGenerationPhase.ImageGenerating || imageSkipped ? "completed" : "pending"),
      new(BlogGenerationPhase.Ready, "Ready for editor", finalPhase == BlogGenerationPhase.Ready ? "completed" : "pending")
    ];
  }
}
