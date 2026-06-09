using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Services;

public sealed class BlogPostService(IBlogPostRepository blogPostRepository, IStorageService storageService) : IBlogPostService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  public async Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(
    BlogPostStatus? status,
    string? tag,
    string? search,
    int page,
    int pageSize,
    bool includeDeleted = false,
    CancellationToken cancellationToken = default)
  {
    var validPage = page <= 0 ? 1 : page;
    var validPageSize = pageSize is <= 0 or > 50 ? 12 : pageSize;
    var (items, totalCount) = await blogPostRepository.GetAllAsync(status, tag, search, validPage, validPageSize, includeDeleted, cancellationToken);
    var mapped = await Task.WhenAll(items.Select(item => MapListItemAsync(item, cancellationToken)));
    return new PagedResult<BlogPostListItemDto>(mapped.ToList(), totalCount, validPage, validPageSize);
  }

  public async Task<BlogPostDto?> GetBySlugAsync(string slug, bool includeDrafts = false, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(slug)) return null;
    var post = await blogPostRepository.GetBySlugAsync(slug.Trim(), includeDrafts, cancellationToken);
    return post is null ? null : await MapPostAsync(post, cancellationToken);
  }

  public async Task<BlogPostDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
  {
    var post = await blogPostRepository.GetByIdAsync(id, includeDeleted, cancellationToken);
    return post is null ? null : await MapPostAsync(post, cancellationToken);
  }

  public async Task<IReadOnlyList<BlogPostListItemDto>> GetRelatedAsync(string slug, int count, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(slug)) return [];
    var post = await blogPostRepository.GetBySlugAsync(slug.Trim(), false, cancellationToken);
    if (post is null) return [];

    var validCount = count is <= 0 or > 6 ? 3 : count;
    var related = await blogPostRepository.GetRelatedAsync(post.Id, DeserializeTags(post.Tags), validCount, cancellationToken);
    var mapped = await Task.WhenAll(related.Select(item => MapListItemAsync(item, cancellationToken)));
    return mapped.ToList();
  }

  public Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    => blogPostRepository.GetAllTagsAsync(cancellationToken);

  public async Task<BlogPostDto> CreateAsync(CreateBlogPostRequest request, CancellationToken cancellationToken = default)
  {
    ValidateRequest(request.Title, request.Excerpt, request.Content, request.CanonicalUrl);
    var status = request.Status;
    var now = DateTime.UtcNow;
    var normalizedFeaturedImage = await NormalizeFeaturedImageForStatusAsync(request.FeaturedImage, status, cancellationToken);
    var normalizedContent = await NormalizeContentForStatusAsync(request.Content.GetRawText(), status, cancellationToken);
    var post = new BlogPost
    {
      Title = request.Title.Trim(),
      Slug = BuildSlug(request.Slug, request.Title),
      Excerpt = request.Excerpt.Trim(),
      FeaturedImage = normalizedFeaturedImage,
      FeaturedImageWidth = request.FeaturedImageWidth,
      FeaturedImageHeight = request.FeaturedImageHeight,
      Template = request.Template,
      Content = normalizedContent,
      Tags = JsonSerializer.Serialize(NormalizeTags(request.Tags), JsonOptions),
      AuthorId = request.AuthorId,
      AuthorNameOverride = NormalizeOptional(request.AuthorNameOverride),
      AuthorBio = NormalizeOptional(request.AuthorBio),
      ReviewedBy = NormalizeOptional(request.ReviewedBy),
      InformationGain = NormalizeOptional(request.InformationGain),
      Status = status,
      PublishedAt = status == BlogPostStatus.Published ? request.PublishedAt ?? now : request.PublishedAt,
      MetaTitle = NormalizeOptional(request.MetaTitle),
      MetaDescription = NormalizeOptional(request.MetaDescription),
      CanonicalUrl = NormalizeOptional(request.CanonicalUrl),
      CreatedAt = now,
      UpdatedAt = now
    };

    await blogPostRepository.AddAsync(post, cancellationToken);
    var created = await blogPostRepository.GetByIdAsync(post.Id, false, cancellationToken) ?? post;
    return await MapPostAsync(created, cancellationToken);
  }

  public async Task<BlogPostDto> UpdateAsync(Guid id, UpdateBlogPostRequest request, CancellationToken cancellationToken = default)
  {
    ValidateRequest(request.Title, request.Excerpt, request.Content, request.CanonicalUrl);
    var post = await blogPostRepository.GetByIdAsync(id, true, cancellationToken)
      ?? throw new InvalidOperationException("Không tìm thấy bài viết.");

    var normalizedFeaturedImage = await NormalizeFeaturedImageForStatusAsync(request.FeaturedImage, request.Status, cancellationToken);
    var normalizedContent = await NormalizeContentForStatusAsync(request.Content.GetRawText(), request.Status, cancellationToken);

    post.Title = request.Title.Trim();
    post.Slug = BuildSlug(request.Slug, request.Title);
    post.Excerpt = request.Excerpt.Trim();
    post.FeaturedImage = normalizedFeaturedImage;
    post.FeaturedImageWidth = request.FeaturedImageWidth;
    post.FeaturedImageHeight = request.FeaturedImageHeight;
    post.Template = request.Template;
    post.Content = normalizedContent;
    post.Tags = JsonSerializer.Serialize(NormalizeTags(request.Tags), JsonOptions);
    post.AuthorId = request.AuthorId;
    post.AuthorNameOverride = NormalizeOptional(request.AuthorNameOverride);
    post.AuthorBio = NormalizeOptional(request.AuthorBio);
    post.ReviewedBy = NormalizeOptional(request.ReviewedBy);
    post.InformationGain = NormalizeOptional(request.InformationGain);
    post.Status = request.Status;
    post.PublishedAt = request.Status == BlogPostStatus.Published ? request.PublishedAt ?? post.PublishedAt ?? DateTime.UtcNow : request.PublishedAt;
    post.MetaTitle = NormalizeOptional(request.MetaTitle);
    post.MetaDescription = NormalizeOptional(request.MetaDescription);
    post.CanonicalUrl = NormalizeOptional(request.CanonicalUrl);
    post.UpdatedAt = DateTime.UtcNow;
    post.IsDeleted = false;
    post.DeletedAt = null;

    await blogPostRepository.UpdateAsync(post, cancellationToken);
    var updated = await blogPostRepository.GetByIdAsync(post.Id, false, cancellationToken) ?? post;
    return await MapPostAsync(updated, cancellationToken);
  }

  public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    => blogPostRepository.SoftDeleteAsync(id, cancellationToken);

  public async Task<string> BuildBlogSitemapAsync(string siteBaseUrl, CancellationToken cancellationToken = default)
  {
    var (items, _) = await blogPostRepository.GetAllAsync(BlogPostStatus.Published, null, null, 1, 50000, false, cancellationToken);
    var baseUrl = siteBaseUrl.TrimEnd('/');
    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    foreach (var post in items)
    {
      sb.AppendLine("  <url>");
      sb.AppendLine($"    <loc>{baseUrl}/blog/{SecurityElementEscape(post.Slug)}/</loc>");
      sb.AppendLine($"    <lastmod>{post.UpdatedAt:yyyy-MM-dd}</lastmod>");
      sb.AppendLine("    <changefreq>weekly</changefreq>");
      sb.AppendLine("    <priority>0.7</priority>");
      sb.AppendLine("  </url>");
    }
    sb.AppendLine("</urlset>");
    return sb.ToString();
  }

  public async Task<string> BuildLlmsTextAsync(string siteBaseUrl, CancellationToken cancellationToken = default)
  {
    var (items, _) = await blogPostRepository.GetAllAsync(BlogPostStatus.Published, null, null, 1, 20, false, cancellationToken);
    var baseUrl = siteBaseUrl.TrimEnd('/');
    var sb = new StringBuilder();
    sb.AppendLine("# Áo Dài Nhà Uyên");
    sb.AppendLine("> Premium áo dài Việt Nam — blog về thời trang, văn hóa, đám cưới");
    sb.AppendLine();
    sb.AppendLine("## Bài Viết Mới Nhất");
    foreach (var post in items)
    {
      sb.AppendLine($"- [{post.Title}]({baseUrl}/blog/{post.Slug}/): {post.Excerpt}");
    }
    return sb.ToString();
  }

  private async Task<string?> NormalizeFeaturedImageForStatusAsync(string? value, BlogPostStatus status, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return status == BlogPostStatus.Published
      ? await PromoteBlogImageReferenceAsync(value.Trim(), cancellationToken)
      : await DemoteBlogImageReferenceAsync(value.Trim(), cancellationToken);
  }

  private async Task<string> NormalizeContentForStatusAsync(string rawContent, BlogPostStatus status, CancellationToken cancellationToken)
  {
    using var document = JsonDocument.Parse(rawContent);
    using var stream = new MemoryStream();
    await using (var writer = new Utf8JsonWriter(stream))
    {
      await RewriteElementAsync(writer, document.RootElement, status, cancellationToken);
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  private async Task RewriteElementAsync(Utf8JsonWriter writer, JsonElement element, BlogPostStatus status, CancellationToken cancellationToken)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject())
        {
          writer.WritePropertyName(property.Name);
          if (property.Name is "src" && property.Value.ValueKind == JsonValueKind.String)
          {
            var value = property.Value.GetString() ?? string.Empty;
            var next = status == BlogPostStatus.Published
              ? await PromoteBlogImageReferenceAsync(value, cancellationToken)
              : await DemoteBlogImageReferenceAsync(value, cancellationToken);
            writer.WriteStringValue(next);
          }
          else
          {
            await RewriteElementAsync(writer, property.Value, status, cancellationToken);
          }
        }
        writer.WriteEndObject();
        break;
      case JsonValueKind.Array:
        writer.WriteStartArray();
        foreach (var item in element.EnumerateArray())
        {
          await RewriteElementAsync(writer, item, status, cancellationToken);
        }
        writer.WriteEndArray();
        break;
      default:
        element.WriteTo(writer);
        break;
    }
  }

  private async Task<string> PromoteBlogImageReferenceAsync(string value, CancellationToken cancellationToken)
  {
    if (!TryGetPrivateBlogKey(value, out var privateKey)) return value;
    var publicUrl = await storageService.CopyToPublicBlogAsync(privateKey, cancellationToken);
    return publicUrl;
  }

  private async Task<string> DemoteBlogImageReferenceAsync(string value, CancellationToken cancellationToken)
  {
    if (TryGetPrivateBlogKey(value, out var privateKey)) return privateKey;
    if (!TryGetPublicBlogKey(value, out var publicKey)) return value;
    await storageService.DeleteAsync(publicKey, cancellationToken);
    var fileName = publicKey[(publicKey.LastIndexOf('/') + 1)..];
    return $"aodainhauyen/private/blog/{fileName}";
  }

  private static bool TryGetPrivateBlogKey(string value, out string objectKey)
  {
    objectKey = string.Empty;
    var marker = "aodainhauyen/private/blog/";
    var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
    if (index < 0) return false;
    objectKey = value[index..].Split('?', '#')[0].Trim('/');
    return true;
  }

  private static bool TryGetPublicBlogKey(string value, out string objectKey)
  {
    objectKey = string.Empty;
    var marker = "aodainhauyen/public/blog/";
    var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
    if (index < 0) return false;
    objectKey = value[index..].Split('?', '#')[0].Trim('/');
    return true;
  }

  private async Task<string?> ResolveFeaturedImageAsync(string? value, BlogPostStatus status, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    if (TryGetPrivateBlogKey(value, out var privateKey))
    {
      return await storageService.GeneratePresignedGetUrlAsync(privateKey, 86400, cancellationToken);
    }

    if (TryGetPublicBlogKey(value, out var publicKey))
    {
      return storageService.BuildCanonicalUrl(publicKey);
    }

    return value;
  }

  private async Task<IReadOnlyList<BlogBlockDto>> DeserializeBlocksAsync(string json, BlogPostStatus status, CancellationToken cancellationToken)
  {
    var normalized = status == BlogPostStatus.Published ? json : await ResolvePrivateImageRefsAsync(json, cancellationToken);
    return DeserializeBlocks(normalized);
  }

  private async Task<string> ResolvePrivateImageRefsAsync(string rawContent, CancellationToken cancellationToken)
  {
    using var document = JsonDocument.Parse(rawContent);
    using var stream = new MemoryStream();
    await using (var writer = new Utf8JsonWriter(stream))
    {
      await RewritePrivateRefsForReadAsync(writer, document.RootElement, cancellationToken);
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  private async Task RewritePrivateRefsForReadAsync(Utf8JsonWriter writer, JsonElement element, CancellationToken cancellationToken)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject())
        {
          writer.WritePropertyName(property.Name);
          if (property.Name is "src" && property.Value.ValueKind == JsonValueKind.String && TryGetPrivateBlogKey(property.Value.GetString() ?? string.Empty, out var privateKey))
          {
            writer.WriteStringValue(await storageService.GeneratePresignedGetUrlAsync(privateKey, 86400, cancellationToken));
          }
          else
          {
            await RewritePrivateRefsForReadAsync(writer, property.Value, cancellationToken);
          }
        }
        writer.WriteEndObject();
        break;
      case JsonValueKind.Array:
        writer.WriteStartArray();
        foreach (var item in element.EnumerateArray())
        {
          await RewritePrivateRefsForReadAsync(writer, item, cancellationToken);
        }
        writer.WriteEndArray();
        break;
      default:
        element.WriteTo(writer);
        break;
    }
  }

  private static void ValidateRequest(string title, string excerpt, JsonElement content, string? canonicalUrl)
  {
    if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Tiêu đề bài viết không được để trống.");
    if (string.IsNullOrWhiteSpace(excerpt)) throw new ArgumentException("Tóm tắt bài viết không được để trống.");
    if (content.ValueKind != JsonValueKind.Array) throw new ArgumentException("Nội dung bài viết phải là danh sách block JSON.");
    if (!string.IsNullOrWhiteSpace(canonicalUrl))
    {
      if (!Uri.TryCreate(canonicalUrl, UriKind.Absolute, out var canonicalUri))
        throw new ArgumentException("Canonical URL không hợp lệ.");
      if (!string.Equals(canonicalUri.Host, "aodainhauyen.io.vn", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Canonical URL phải thuộc aodainhauyen.io.vn.");
    }

    foreach (var block in content.EnumerateArray())
    {
      ValidateBlock(block);
    }
  }

  private static void ValidateBlock(JsonElement block)
  {
    if (block.ValueKind != JsonValueKind.Object) throw new ArgumentException("Block nội dung phải là object JSON.");
    var type = RequiredString(block, "type", "Block thiếu type.");
    switch (type)
    {
      case "heading":
        var level = RequiredInt(block, "level", "Heading thiếu level.");
        if (level is < 1 or > 3) throw new ArgumentException("Heading level phải từ 1 đến 3.");
        RequiredString(block, "content", "Heading thiếu nội dung.");
        break;
      case "paragraph":
        RequiredString(block, "content", "Đoạn văn thiếu nội dung.");
        break;
      case "image":
        RequiredString(block, "src", "Ảnh thiếu đường dẫn.");
        ValidateAlt(RequiredString(block, "alt", "Ảnh thiếu alt text."));
        break;
      case "gallery":
        ValidateGallery(block);
        break;
      case "video":
        ValidateAllowedUrl(RequiredString(block, "src", "Video thiếu đường dẫn."), AllowedVideoPrefixes, "Video chỉ được dùng nguồn nội bộ.");
        break;
      case "product_spotlight":
        ValidateStringArray(block, "productSlugs", "Product spotlight thiếu slug sản phẩm.");
        break;
      case "step":
        if (RequiredInt(block, "stepNumber", "Step thiếu số thứ tự.") <= 0) throw new ArgumentException("Số thứ tự step phải lớn hơn 0.");
        RequiredString(block, "title", "Step thiếu tiêu đề.");
        RequiredString(block, "content", "Step thiếu nội dung.");
        break;
      case "quote":
        RequiredString(block, "content", "Quote thiếu nội dung.");
        break;
      case "divider":
        break;
      case "callout":
        var variant = RequiredString(block, "variant", "Callout thiếu variant.");
        if (variant is not ("info" or "warning" or "tip")) throw new ArgumentException("Callout variant không hợp lệ.");
        RequiredString(block, "content", "Callout thiếu nội dung.");
        break;
      case "code":
        RequiredString(block, "language", "Code block thiếu language.");
        RequiredString(block, "content", "Code block thiếu nội dung.");
        break;
      case "embed":
        ValidateAllowedUrl(RequiredString(block, "url", "Embed thiếu URL."), AllowedEmbedPrefixes, "Embed URL không được phép.");
        break;
      default:
        throw new ArgumentException($"Block type không được hỗ trợ: {type}.");
    }
  }

  private static readonly string[] AllowedEmbedPrefixes = [
    "https://www.youtube.com/embed/",
    "https://player.vimeo.com/video/"
  ];

  private static readonly string[] AllowedVideoPrefixes = [
    "https://aodainhauyen.io.vn/",
    "https://api-hk1.aodainhauyen.io.vn/",
    "https://api-us1.aodainhauyen.io.vn/",
    "/upload/",
    "/uploads/"
  ];

  private static void ValidateGallery(JsonElement block)
  {
    if (!block.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array || images.GetArrayLength() == 0)
      throw new ArgumentException("Gallery phải có ít nhất một ảnh.");
    foreach (var image in images.EnumerateArray())
    {
      RequiredString(image, "src", "Ảnh gallery thiếu đường dẫn.");
      ValidateAlt(RequiredString(image, "alt", "Ảnh gallery thiếu alt text."));
    }
  }

  private static void ValidateStringArray(JsonElement block, string propertyName, string message)
  {
    if (!block.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array || values.GetArrayLength() == 0)
      throw new ArgumentException(message);
    foreach (var value in values.EnumerateArray())
    {
      if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString())) throw new ArgumentException(message);
    }
  }

  private static void ValidateAlt(string alt)
  {
    if (alt.Length is < 10 or > 125) throw new ArgumentException("Alt text ảnh phải dài 10-125 ký tự.");
  }

  private static void ValidateAllowedUrl(string value, IReadOnlyList<string> prefixes, string message)
  {
    if (!prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException(message);
  }

  private static string RequiredString(JsonElement element, string propertyName, string message)
  {
    if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
      throw new ArgumentException(message);
    return value.GetString()!.Trim();
  }

  private static int RequiredInt(JsonElement element, string propertyName, string message)
  {
    if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt32(out var number)) throw new ArgumentException(message);
    return number;
  }

  private async Task<BlogPostListItemDto> MapListItemAsync(BlogPost post, CancellationToken cancellationToken) => new(
    post.Id,
    post.Title,
    post.Slug,
    post.Excerpt,
    await ResolveFeaturedImageAsync(post.FeaturedImage, post.Status, cancellationToken),
    post.FeaturedImageWidth,
    post.FeaturedImageHeight,
    post.Template,
    DeserializeTags(post.Tags),
    post.AuthorNameOverride ?? post.Author?.FullName,
    post.Status,
    post.PublishedAt,
    post.UpdatedAt);

  private async Task<BlogPostDto> MapPostAsync(BlogPost post, CancellationToken cancellationToken) => new(
    post.Id,
    post.Title,
    post.Slug,
    post.Excerpt,
    await ResolveFeaturedImageAsync(post.FeaturedImage, post.Status, cancellationToken),
    post.FeaturedImageWidth,
    post.FeaturedImageHeight,
    post.Template,
    await DeserializeBlocksAsync(post.Content, post.Status, cancellationToken),
    DeserializeTags(post.Tags),
    post.AuthorId,
    post.AuthorNameOverride ?? post.Author?.FullName,
    post.Author?.AvatarUrl,
    post.AuthorBio,
    post.ReviewedBy,
    post.InformationGain,
    post.Status,
    post.PublishedAt,
    post.MetaTitle,
    post.MetaDescription,
    post.CanonicalUrl,
    post.CreatedAt,
    post.UpdatedAt);

  private static IReadOnlyList<BlogBlockDto> DeserializeBlocks(string json)
  {
    using var doc = JsonDocument.Parse(json);
    var blocks = new List<BlogBlockDto>();
    foreach (var element in doc.RootElement.EnumerateArray())
    {
      var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? "unknown" : "unknown";
      blocks.Add(new BlogBlockDto(type, element.Clone()));
    }
    return blocks;
  }

  private static IReadOnlyList<string> DeserializeTags(string json)
  {
    try { return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? []; }
    catch { return []; }
  }

  private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    => tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();

  private static string? NormalizeOptional(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static string BuildSlug(string? slug, string title)
  {
    var source = string.IsNullOrWhiteSpace(slug) ? title : slug;
    var normalized = source.Normalize(NormalizationForm.FormD);
    var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
    var ascii = new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    ascii = ascii.Replace('đ', 'd');
    ascii = Regex.Replace(ascii, @"[^a-z0-9\s-]", "");
    ascii = Regex.Replace(ascii, @"[\s-]+", "-").Trim('-');
    return string.IsNullOrWhiteSpace(ascii) ? Guid.NewGuid().ToString("N") : ascii[..Math.Min(ascii.Length, 500)];
  }

  private static string SecurityElementEscape(string value)
    => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
