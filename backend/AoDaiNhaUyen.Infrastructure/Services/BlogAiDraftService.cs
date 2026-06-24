using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class BlogAiDraftService(
  IAdminLlmProvider llm,
  ILogger<BlogAiDraftService> logger) : IBlogAiDraftService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNameCaseInsensitive = true
  };

  private static readonly HashSet<string> AllowedBlockTypes =
  [
    "heading", "paragraph", "image", "gallery", "video", "product_spotlight",
    "step", "quote", "divider", "callout", "code", "embed"
  ];

  private static readonly object BlogDraftResponseSchema = new
  {
    type = "object",
    properties = new Dictionary<string, object?>
    {
      ["title"] = new { type = "string" },
      ["slug"] = new { type = "string" },
      ["excerpt"] = new { type = "string" },
      ["template"] = new { type = "string" },
      ["content"] = new
      {
        type = "array",
        items = new
        {
          type = "object",
          properties = new Dictionary<string, object?>
          {
            ["type"] = new { type = "string" },
            ["level"] = new { type = "integer" },
            ["content"] = new { type = "string" },
            ["variant"] = new { type = "string" },
            ["stepNumber"] = new { type = "integer" },
            ["title"] = new { type = "string" },
            ["tip"] = new { type = "string" },
            ["productSlugs"] = new { type = "array", items = new { type = "string" } }
          }
        }
      },
      ["tags"] = new { type = "array", items = new { type = "string" } },
      ["metaTitle"] = new { type = "string" },
      ["metaDescription"] = new { type = "string" },
      ["canonicalUrl"] = new { type = "string", nullable = true },
      ["informationGain"] = new { type = "string" },
      ["authorNameOverride"] = new { type = "string" },
      ["authorBio"] = new { type = "string" },
      ["reviewedBy"] = new { type = "string", nullable = true },
      ["blogCategoryId"] = new { type = "string", nullable = true },
      ["qualityWarnings"] = new { type = "array", items = new { type = "string" } },
      ["outline"] = new { type = "array", items = new { type = "string" } },
      ["imagePrompt"] = new { type = "string" }
    },
    required = new[] { "title", "slug", "excerpt", "template", "content", "tags", "metaTitle", "metaDescription", "informationGain", "authorNameOverride", "authorBio", "qualityWarnings", "outline", "imagePrompt" }
  };

  public async Task<GeneratedBlogDraftResponse> GenerateDraftAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken = default)
  {
    var cleanRequest = NormalizeRequest(request);
    Exception? firstJsonError = null;

    try
    {
      var draft = await llm.CompleteJsonAsync<GeneratedBlogDraftModel>(
        BuildSystemPrompt(),
        BuildUserPrompt(cleanRequest),
        BlogDraftResponseSchema,
        cancellationToken);
      return ValidateAndNormalize(draft, cleanRequest);
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException)
    {
      firstJsonError = ex;
      logger.LogWarning(ex, "[BlogAI] Structured JSON draft failed; retrying once with repair prompt.");
    }

    try
    {
      var repaired = await llm.CompleteJsonAsync<GeneratedBlogDraftModel>(
        BuildSystemPrompt(),
        BuildRepairPrompt(cleanRequest, firstJsonError?.Message),
        BlogDraftResponseSchema,
        cancellationToken);
      return ValidateAndNormalize(repaired, cleanRequest);
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException)
    {
      logger.LogWarning(ex, "[BlogAI] Structured JSON repair failed; falling back to stream extraction.");
    }

    try
    {
      var history = new List<AdminLlmMessage>
      {
        new(AdminLlmRole.System, BuildSystemPrompt()),
        new(AdminLlmRole.User, BuildUserPrompt(cleanRequest))
      };

      var sb = new StringBuilder();
      await foreach (var chunk in llm.StreamChatAsync(history, [], cancellationToken))
      {
        if (chunk.Type == "text") sb.Append(chunk.Content);
        if (chunk.Type == "error") logger.LogWarning("[BlogAI] LLM error while generating draft: {Error}", chunk.Content);
      }

      var raw = sb.ToString().Trim();
      if (!string.IsNullOrWhiteSpace(raw))
      {
        var json = ExtractJson(raw);
        var draft = JsonSerializer.Deserialize<GeneratedBlogDraftModel>(json, JsonOptions)
          ?? throw new JsonException("Draft payload was null.");
        return ValidateAndNormalize(draft, cleanRequest);
      }
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException)
    {
      logger.LogWarning(ex, "[BlogAI] Stream JSON extraction failed.");
    }

    return BuildFallbackDraft(cleanRequest, ["AI trả về JSON không hợp lệ sau retry; hệ thống tạo bản nháp khung để chỉnh sửa thủ công."]);
  }

  private static GenerateBlogDraftRequest NormalizeRequest(GenerateBlogDraftRequest request)
  {
    var topic = SafeText(request.Topic, 500);
    if (string.IsNullOrWhiteSpace(topic))
      throw new InvalidOperationException("Chủ đề bài viết không được để trống.");

    var length = (request.Length ?? "standard").Trim().ToLowerInvariant();
    if (length is not ("short" or "standard" or "long")) length = "standard";

    return request with
    {
      Topic = topic,
      TargetKeyword = SafeNullableText(request.TargetKeyword, 200),
      Audience = SafeNullableText(request.Audience, 200),
      Tone = SafeNullableText(request.Tone, 100),
      Length = length,
      Notes = SafeNullableText(request.Notes, 2000),
      ProductSlugs = request.ProductSlugs.Select(s => SafeText(s, 200)).Where(s => !string.IsNullOrWhiteSpace(s)).Take(12).ToList()
    };
  }

  private static string BuildSystemPrompt() => """
Bạn là biên tập viên SEO/E-E-A-T cho website áo dài cao cấp Áo Dài Nhã Uyên.
Chỉ trả về JSON hợp lệ, không markdown, không lời dẫn.
Không bịa số liệu, chứng nhận, địa chỉ, giá, bác sĩ/chuyên gia, hoặc nguồn ngoài nếu không được cung cấp.
Không tạo HTML/script. Mọi nội dung phải là text thuần.
Bài viết phải bằng tiếng Việt, hữu ích cho người đọc, có thông tin độc đáo về chọn/mặc/bảo quản áo dài.
Chủ đề và góc triển khai phải đa dạng, sáng tạo theo ngữ cảnh yêu cầu; không lặp motif cũ, không đóng khung vào một vài mẫu hard-code như mẹo chọn đồ cơ bản. Chủ động mở rộng sang xu hướng cưới, lookbook mùa, chất liệu, màu sắc, nghi thức, phối phụ kiện, bảo quản theo thời tiết, câu chuyện bộ sưu tập, hậu trường may đo, trải nghiệm thử đồ, tình huống sử dụng thực tế và insight phong cách sống khi phù hợp.
Mỗi bài nên có một góc tiếp cận riêng, tiêu đề riêng, outline riêng, informationGain riêng; tránh cảm giác cùng một bài viết đổi mỗi vài từ khóa.
Schema JSON bắt buộc:
{
  "title": "string",
  "slug": "string-khong-dau",
  "excerpt": "string",
  "template": "StandardArticle|PhotoGallery|VideoFeature|ProductSpotlight|HowTo",
  "content": [{ "type": "heading", "level": 2, "content": "..." }, { "type": "paragraph", "content": "..." }],
  "tags": ["string"],
  "metaTitle": "string <= 200",
  "metaDescription": "string <= 500",
  "canonicalUrl": null,
  "informationGain": "string",
  "authorNameOverride": "Ban biên tập Áo Dài Nhã Uyên",
  "authorBio": "string",
  "reviewedBy": "string|null",
  "blogCategoryId": null,
  "qualityWarnings": ["string"],
  "outline": ["string"],
  "imagePrompt": "prompt tạo ảnh minh họa/try-on, không chứa claims sai"
}
Allowed block types: heading(level 1|2|3), paragraph, quote, callout(variant info|warning|tip), step, divider, product_spotlight.
Ưu tiên heading/paragraph/quote/callout; tránh image/video/embed nếu không có URL an toàn.
Quy trình bắt buộc: tạo outline -> soạn nội dung -> tạo imagePrompt -> tự kiểm nội dung trong qualityWarnings.
""";

  private static string BuildUserPrompt(GenerateBlogDraftRequest request) => $"""
Tạo bản nháp blog có cấu trúc BlogBlock[] cho admin duyệt.
Chủ đề: {request.Topic}
Từ khóa chính: {request.TargetKeyword ?? "tự đề xuất"}
Độc giả: {request.Audience ?? "khách hàng quan tâm áo dài"}
Giọng văn: {request.Tone ?? "trang nhã, tư vấn chuyên nghiệp"}
Template: {request.Template}
Độ dài: {request.Length}
Có FAQ: {request.IncludeFaq}
Product slugs liên quan: {string.Join(", ", request.ProductSlugs)}
Ghi chú: {request.Notes ?? "không có"}
Yêu cầu chất lượng:
- Có mở bài rõ lợi ích.
- Có nhiều H2, đoạn ngắn dễ đọc.
- Có `informationGain` nêu giá trị độc đáo.
- Chủ động chọn góc bài sáng tạo, không rập khuôn, không lặp lại chủ đề/mẫu viết quen tay nếu brief cho phép mở rộng.
- Không tái sử dụng máy móc các mô-típ hard-code; mỗi brief cần được diễn giải lại thành concept, nhịp kể và outline riêng.
- Có metaTitle/metaDescription tự nhiên, không nhồi keyword.
- Nếu thiếu dữ liệu thực tế, thêm cảnh báo trong qualityWarnings thay vì bịa.
""";

  private static string BuildRepairPrompt(GenerateBlogDraftRequest request, string? error) => $"""
JSON trước đó không hợp lệ hoặc thiếu schema.
Lỗi cần sửa: {error ?? "không rõ"}

Hãy tạo lại TOÀN BỘ JSON theo đúng schema, không markdown, không lời dẫn.
{BuildUserPrompt(request)}
""";

  private static GeneratedBlogDraftResponse ValidateAndNormalize(GeneratedBlogDraftModel draft, GenerateBlogDraftRequest request)
  {
    var title = SafeText(draft.Title, 500);
    if (string.IsNullOrWhiteSpace(title)) title = request.Topic;

    var blocks = NormalizeBlocks(draft.Content, request);
    var warnings = draft.QualityWarnings?.Select(w => SafeText(w, 300)).Where(w => !string.IsNullOrWhiteSpace(w)).ToList() ?? [];
    AddQualityWarnings(warnings, draft, blocks);

    return new GeneratedBlogDraftResponse(
      title,
      Slugify(string.IsNullOrWhiteSpace(draft.Slug) ? title : draft.Slug),
      SafeText(draft.Excerpt, 1000).DefaultIfBlank($"Bài viết tư vấn về {request.Topic}."),
      Enum.TryParse<BlogPostTemplate>(draft.Template, true, out var template) ? template : request.Template,
      JsonSerializer.SerializeToElement(blocks, JsonOptions),
      (draft.Tags ?? []).Select(t => SafeText(t, 60)).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList(),
      SafeNullableText(draft.MetaTitle, 200) ?? title,
      SafeNullableText(draft.MetaDescription, 500),
      SafeUrlOrNull(draft.CanonicalUrl),
      SafeNullableText(draft.InformationGain, 1000),
      SafeNullableText(draft.AuthorNameOverride, 200) ?? "Ban biên tập Áo Dài Nhã Uyên",
      SafeNullableText(draft.AuthorBio, 1000) ?? "Nội dung được biên tập cho khách hàng quan tâm áo dài, phong cách mặc và bảo quản trang phục truyền thống Việt Nam.",
      SafeNullableText(draft.ReviewedBy, 200),
      draft.BlogCategoryId ?? request.CategoryId,
      warnings,
      BuildOutline(draft, blocks, request),
      BuildImagePrompt(draft, title, request),
      BuildTryOnHandoff(request),
      BuildValidation(warnings, blocks));
  }

  private static List<Dictionary<string, object?>> NormalizeBlocks(JsonElement content, GenerateBlogDraftRequest request)
  {
    if (content.ValueKind != JsonValueKind.Array)
      return FallbackBlocks(request);

    var blocks = new List<Dictionary<string, object?>>();
    foreach (var block in content.EnumerateArray().Take(80))
    {
      if (block.ValueKind != JsonValueKind.Object) continue;
      if (!block.TryGetProperty("type", out var typeEl)) continue;
      var type = typeEl.GetString()?.Trim().ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(type) || !AllowedBlockTypes.Contains(type)) continue;

      var normalized = NormalizeBlock(type, block);
      if (normalized is not null) blocks.Add(normalized);
    }

    return blocks.Count == 0 ? FallbackBlocks(request) : blocks;
  }

  private static Dictionary<string, object?>? NormalizeBlock(string type, JsonElement block)
  {
    string? Str(string name, int max = 2000) => block.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? SafeNullableText(el.GetString(), max) : null;
    int Int(string name, int fallback, int min, int max) => block.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? Math.Clamp(el.GetInt32(), min, max) : fallback;

    return type switch
    {
      "heading" => new() { ["type"] = "heading", ["level"] = Int("level", 2, 1, 3), ["content"] = Str("content", 200) ?? "Tiêu đề" },
      "paragraph" => string.IsNullOrWhiteSpace(Str("content")) ? null : new() { ["type"] = "paragraph", ["content"] = Str("content", 2500) },
      "quote" => string.IsNullOrWhiteSpace(Str("content")) ? null : new() { ["type"] = "quote", ["content"] = Str("content", 800), ["attribution"] = Str("attribution", 200) },
      "callout" => new() { ["type"] = "callout", ["variant"] = SafeVariant(Str("variant", 20)), ["content"] = Str("content", 1000) ?? "Ghi chú cần kiểm tra." },
      "step" => new() { ["type"] = "step", ["stepNumber"] = Int("stepNumber", 1, 1, 50), ["title"] = Str("title", 200) ?? "Bước", ["content"] = Str("content", 1000) ?? "Nội dung bước", ["tip"] = Str("tip", 500) },
      "product_spotlight" => new() { ["type"] = "product_spotlight", ["productSlugs"] = ReadStringArray(block, "productSlugs", 12, 200) },
      "divider" => new() { ["type"] = "divider" },
      "image" => SafeUrlOrNull(Str("src", 1000)) is { } imageUrl ? new() { ["type"] = "image", ["src"] = imageUrl, ["alt"] = Str("alt", 200) ?? "Ảnh minh họa áo dài", ["caption"] = Str("caption", 300), ["width"] = "contained" } : null,
      "gallery" or "video" or "code" or "embed" => null,
      _ => null
    };
  }

  private static GeneratedBlogDraftResponse BuildFallbackDraft(GenerateBlogDraftRequest request, IReadOnlyList<string> warnings)
  {
    var blocks = FallbackBlocks(request);
    return new GeneratedBlogDraftResponse(
      request.Topic,
      Slugify(request.Topic),
      $"Gợi ý nội dung về {request.Topic} cho khách hàng quan tâm áo dài.",
      request.Template,
      JsonSerializer.SerializeToElement(blocks, JsonOptions),
      ["áo dài", request.TargetKeyword ?? request.Topic],
      request.Topic,
      $"Tìm hiểu {request.Topic} cùng Áo Dài Nhã Uyên: gợi ý chọn, mặc và bảo quản áo dài phù hợp.",
      null,
      "Bản nháp cần bổ sung ví dụ thực tế, hình ảnh sản phẩm và kiểm duyệt thủ công trước khi xuất bản.",
      "Ban biên tập Áo Dài Nhã Uyên",
      "Nội dung được biên tập cho khách hàng quan tâm áo dài, phong cách mặc và bảo quản trang phục truyền thống Việt Nam.",
      null,
      request.CategoryId,
      warnings,
      BuildFallbackOutline(request),
      BuildImagePrompt(null, request.Topic, request),
      BuildTryOnHandoff(request),
      BuildValidation(warnings, blocks));
  }

  private static List<Dictionary<string, object?>> FallbackBlocks(GenerateBlogDraftRequest request) =>
  [
    new() { ["type"] = "heading", ["level"] = 2, ["content"] = request.Topic },
    new() { ["type"] = "paragraph", ["content"] = $"Bài viết này là bản nháp AI về {request.Topic}. Admin cần bổ sung trải nghiệm thực tế, hình ảnh và kiểm tra thông tin trước khi xuất bản." },
    new() { ["type"] = "heading", ["level"] = 2, ["content"] = "Các ý chính cần triển khai" },
    new() { ["type"] = "paragraph", ["content"] = "Giới thiệu nhu cầu của người đọc, tiêu chí chọn áo dài phù hợp, lỗi thường gặp và lời khuyên bảo quản sau khi sử dụng." },
    new() { ["type"] = "callout", ["variant"] = "warning", ["content"] = "Bản nháp cần được người phụ trách nội dung kiểm duyệt trước khi xuất bản." }
  ];

  private static IReadOnlyList<string> BuildOutline(
    GeneratedBlogDraftModel draft,
    List<Dictionary<string, object?>> blocks,
    GenerateBlogDraftRequest request)
  {
    var outline = (draft.Outline ?? [])
      .Select(item => SafeText(item, 160))
      .Where(item => !string.IsNullOrWhiteSpace(item))
      .Take(10)
      .ToList();

    if (outline.Count > 0) return outline;

    outline = blocks
      .Where(b => b.TryGetValue("type", out var type) && Equals(type, "heading"))
      .Select(b => b.TryGetValue("content", out var content) ? SafeText(content?.ToString(), 160) : string.Empty)
      .Where(item => !string.IsNullOrWhiteSpace(item))
      .Take(10)
      .ToList();

    return outline.Count > 0 ? outline : BuildFallbackOutline(request);
  }

  private static IReadOnlyList<string> BuildFallbackOutline(GenerateBlogDraftRequest request) =>
  [
    request.Topic,
    "Vấn đề/nhu cầu của người đọc",
    "Gợi ý chọn áo dài phù hợp",
    "Cách phối và bảo quản",
    "Checklist trước khi xuất bản"
  ];

  private static string BuildImagePrompt(GeneratedBlogDraftModel? draft, string title, GenerateBlogDraftRequest request)
  {
    var provided = SafeNullableText(draft?.ImagePrompt, 1000);
    if (!string.IsNullOrWhiteSpace(provided)) return provided;

    var keyword = request.TargetKeyword ?? request.Topic;
    return $"Premium Vietnamese áo dài editorial image for blog '{title}', focus on {keyword}, elegant silk texture, soft natural light, refined boutique styling, no text overlay, no false brand claims.";
  }

  private static BlogTryOnHandoffDto BuildTryOnHandoff(GenerateBlogDraftRequest request) =>
    new(
      "http://localhost:5173/ai-tryon",
      "/api/v1/ai-tryon",
      "needs_admin_image",
      ["personImage", "garmentProductId"],
      request.ProductSlugs.FirstOrDefault(),
      null,
      "Backend tool không mở frontend page trực tiếp. Mở URL này, upload ảnh người mẫu/khách và chọn sản phẩm để trigger API thử đồ.");

  private static BlogDraftValidationDto BuildValidation(IReadOnlyList<string> warnings, List<Dictionary<string, object?>> blocks)
  {
    var checks = new List<string>
    {
      "outline_generated",
      "draft_content_generated",
      "image_prompt_generated",
      "tryon_handoff_prepared",
      "content_sanitized"
    };

    if (blocks.Any(b => b.TryGetValue("type", out var t) && Equals(t, "heading"))) checks.Add("headings_present");
    if (blocks.Any(b => b.TryGetValue("type", out var t) && Equals(t, "paragraph"))) checks.Add("paragraphs_present");

    return new BlogDraftValidationDto(warnings.Count == 0, warnings, checks);
  }

  private static void AddQualityWarnings(List<string> warnings, GeneratedBlogDraftModel draft, List<Dictionary<string, object?>> blocks)
  {
    if (!blocks.Any(b => b.TryGetValue("type", out var t) && Equals(t, "heading"))) warnings.Add("Bài viết nên có ít nhất một heading H2.");
    if (blocks.Count(b => b.TryGetValue("type", out var t) && Equals(t, "paragraph")) < 3) warnings.Add("Bài viết còn ít đoạn nội dung; nên kiểm tra độ sâu trước khi xuất bản.");
    if (string.IsNullOrWhiteSpace(draft.InformationGain)) warnings.Add("Thiếu informationGain: cần nêu giá trị độc đáo/kinh nghiệm thực tế.");
    if (string.IsNullOrWhiteSpace(draft.ReviewedBy)) warnings.Add("Nên có người kiểm duyệt trước khi xuất bản nội dung AI.");
  }

  private static string ExtractJson(string raw)
  {
    var trimmed = raw.Trim();
    if (trimmed.StartsWith("```", StringComparison.Ordinal))
    {
      var match = Regex.Match(trimmed, "```(?:json)?\\s*(?<json>[\\s\\S]*?)\\s*```", RegexOptions.IgnoreCase);
      if (match.Success) return match.Groups["json"].Value.Trim();
    }

    var start = trimmed.IndexOf('{');
    var end = trimmed.LastIndexOf('}');
    return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
  }

  private static IReadOnlyList<string> ReadStringArray(JsonElement block, string name, int maxItems, int maxLen)
  {
    if (!block.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array) return [];
    return el.EnumerateArray()
      .Where(x => x.ValueKind == JsonValueKind.String)
      .Select(x => SafeText(x.GetString() ?? string.Empty, maxLen))
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Take(maxItems)
      .ToList();
  }

  private static string SafeVariant(string? value) => value?.ToLowerInvariant() is "info" or "warning" or "tip" ? value.ToLowerInvariant() : "tip";
  private static string? SafeUrlOrNull(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? value : null;
  private static string SafeText(string? value, int max) => StripUnsafe(value ?? string.Empty).Trim().Truncate(max);
  private static string? SafeNullableText(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : SafeText(value, max);
  private static string StripUnsafe(string value) => value.Replace("<script", "script", StringComparison.OrdinalIgnoreCase).Replace("</script", "/script", StringComparison.OrdinalIgnoreCase).Replace("javascript:", string.Empty, StringComparison.OrdinalIgnoreCase);

  private static string Slugify(string text)
  {
    var normalized = text.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var c in normalized)
    {
      var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
      if (category == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
      var lower = char.ToLowerInvariant(c == 'đ' || c == 'Đ' ? 'd' : c);
      sb.Append(char.IsLetterOrDigit(lower) ? lower : '-');
    }
    return Regex.Replace(sb.ToString(), "-+", "-").Trim('-').Truncate(500).DefaultIfBlank("bai-viet");
  }

  private sealed record GeneratedBlogDraftModel
  {
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Excerpt { get; init; }
    public string? Template { get; init; }
    public JsonElement Content { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? InformationGain { get; init; }
    public string? AuthorNameOverride { get; init; }
    public string? AuthorBio { get; init; }
    public string? ReviewedBy { get; init; }
    public Guid? BlogCategoryId { get; init; }
    public IReadOnlyList<string>? QualityWarnings { get; init; }
    public IReadOnlyList<string>? Outline { get; init; }
    public string? ImagePrompt { get; init; }
  }
}

internal static class BlogAiDraftStringExtensions
{
  public static string Truncate(this string value, int max) => value.Length <= max ? value : value[..max];
  public static string DefaultIfBlank(this string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
