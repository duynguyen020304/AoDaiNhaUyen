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

  public async Task<GeneratedBlogDraftResponse> GenerateDraftAsync(
    GenerateBlogDraftRequest request,
    CancellationToken cancellationToken = default)
  {
    var cleanRequest = NormalizeRequest(request);
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
    if (string.IsNullOrWhiteSpace(raw))
      return BuildFallbackDraft(cleanRequest, ["AI không trả về nội dung; hệ thống tạo bản nháp khung để chỉnh sửa thủ công."]);

    try
    {
      var json = ExtractJson(raw);
      var draft = JsonSerializer.Deserialize<GeneratedBlogDraftModel>(json, JsonOptions)
        ?? throw new JsonException("Draft payload was null.");
      return ValidateAndNormalize(draft, cleanRequest);
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException)
    {
      logger.LogWarning(ex, "[BlogAI] Invalid draft JSON. Falling back to safe draft.");
      return BuildFallbackDraft(cleanRequest, ["AI trả về JSON không hợp lệ; hệ thống tạo bản nháp khung để chỉnh sửa thủ công."]);
    }
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
  "qualityWarnings": ["string"]
}
Allowed block types: heading(level 1|2|3), paragraph, quote, callout(variant info|warning|tip), step, divider, product_spotlight.
Ưu tiên heading/paragraph/quote/callout; tránh image/video/embed nếu không có URL an toàn.
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
- Có metaTitle/metaDescription tự nhiên, không nhồi keyword.
- Nếu thiếu dữ liệu thực tế, thêm cảnh báo trong qualityWarnings thay vì bịa.
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
      warnings);
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
      warnings);
  }

  private static List<Dictionary<string, object?>> FallbackBlocks(GenerateBlogDraftRequest request) =>
  [
    new() { ["type"] = "heading", ["level"] = 2, ["content"] = request.Topic },
    new() { ["type"] = "paragraph", ["content"] = $"Bài viết này là bản nháp AI về {request.Topic}. Admin cần bổ sung trải nghiệm thực tế, hình ảnh và kiểm tra thông tin trước khi xuất bản." },
    new() { ["type"] = "heading", ["level"] = 2, ["content"] = "Các ý chính cần triển khai" },
    new() { ["type"] = "paragraph", ["content"] = "Giới thiệu nhu cầu của người đọc, tiêu chí chọn áo dài phù hợp, lỗi thường gặp và lời khuyên bảo quản sau khi sử dụng." },
    new() { ["type"] = "callout", ["variant"] = "warning", ["content"] = "Bản nháp cần được người phụ trách nội dung kiểm duyệt trước khi xuất bản." }
  ];

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
  }
}

internal static class BlogAiDraftStringExtensions
{
  public static string Truncate(this string value, int max) => value.Length <= max ? value : value[..max];
  public static string DefaultIfBlank(this string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
