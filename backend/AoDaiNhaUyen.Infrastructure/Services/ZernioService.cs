using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ZernioService(
  IHttpClientFactory httpClientFactory,
  IOptions<ZernioSettings> zernioOptions,
  AppDbContext dbContext,
  ILogger<ZernioService> logger) : ISocialService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly ZernioSettings settings = zernioOptions.Value;

  public async Task<IReadOnlyList<SocialAccountConnectionDto>> GetAccountsAsync(
    string? platform = null,
    bool sync = false,
    string? profileId = null,
    CancellationToken cancellationToken = default)
  {
    if (sync)
    {
      await SyncAccountsAsync(platform, profileId, cancellationToken);
    }

    var query = dbContext.SocialAccountConnections.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(platform))
    {
      var normalizedPlatform = NormalizePlatform(platform);
      query = query.Where(x => x.Platform == normalizedPlatform);
    }

    if (!string.IsNullOrWhiteSpace(profileId))
    {
      var normalizedProfileId = profileId.Trim();
      query = query.Where(x => x.ZernioProfileId == normalizedProfileId);
    }

    return await query
      .OrderByDescending(x => x.IsActive)
      .ThenBy(x => x.DisplayName ?? x.Username ?? x.ZernioAccountId)
      .Select(x => new SocialAccountConnectionDto(
        x.Id,
        x.Provider,
        x.Platform,
        x.ZernioProfileId,
        x.ZernioAccountId,
        x.DisplayName,
        x.Username,
        x.AvatarUrl,
        x.LastSyncedAt,
        x.IsActive))
      .ToListAsync(cancellationToken);
  }

  public async Task<SocialConnectUrlDto> GetConnectUrlAsync(
    CreateSocialConnectUrlRequest request,
    CancellationToken cancellationToken = default)
  {
    var platform = NormalizePlatform(request.Platform);
    var profileId = NormalizeRequired(request.ProfileId, "profileId");
    var redirectUrl = NormalizeAbsoluteUri(request.RedirectUrl, "redirectUrl");
    var query = new Dictionary<string, string>
    {
      ["profileId"] = profileId,
      ["redirect_url"] = redirectUrl,
    };

    if (request.Headless)
    {
      query["headless"] = "true";
    }

    var response = await SendAsync<ZernioConnectUrlResponse>(
      HttpMethod.Get,
      $"connect/{Uri.EscapeDataString(platform)}",
      query,
      null,
      cancellationToken);

    return new SocialConnectUrlDto(
      NormalizeRequired(response.AuthUrl, "authUrl"),
      response.State);
  }

  public async Task<IReadOnlyList<SocialAccountConnectionDto>> SelectFacebookPageAsync(
    SelectFacebookPageRequest request,
    CancellationToken cancellationToken = default)
  {
    var profileId = NormalizeRequired(request.ProfileId, "profileId");
    var body = new
    {
      profileId,
      pageId = NormalizeRequired(request.PageId, "pageId"),
      tempToken = NormalizeRequired(request.TempToken, "tempToken"),
      userProfile = new
      {
        id = NormalizeRequired(request.UserProfile.Id, "userProfile.id"),
        name = NormalizeRequired(request.UserProfile.Name, "userProfile.name"),
        profilePicture = NormalizeRequired(request.UserProfile.ProfilePicture, "userProfile.profilePicture")
      },
      redirect_url = NormalizeAbsoluteUri(request.RedirectUrl, "redirectUrl")
    };

    await SendAsync<JsonElement>(HttpMethod.Post, "connect/facebook/select-page", null, body, cancellationToken);
    return await GetAccountsAsync("facebook", sync: true, profileId, cancellationToken);
  }

  public async Task DisconnectAccountAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var connection = await dbContext.SocialAccountConnections
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (connection is null)
    {
      return;
    }

    connection.IsActive = false;
    connection.IsDeleted = true;
    connection.DeletedAt = DateTime.UtcNow;
    connection.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<SocialPostDto> CreatePostAsync(
    CreateSocialPostRequest request,
    CancellationToken cancellationToken = default)
  {
    var content = NormalizeRequired(request.Content, "content");
    if (request.AccountIds.Count == 0)
    {
      throw new ZernioApiException("Vui lòng chọn ít nhất một fanpage.", "zernio_account_required", 400);
    }

    var accounts = await dbContext.SocialAccountConnections
      .AsNoTracking()
      .Where(x => request.AccountIds.Contains(x.Id) && x.IsActive)
      .ToListAsync(cancellationToken);

    if (accounts.Count != request.AccountIds.Count)
    {
      throw new ZernioApiException("Một hoặc nhiều fanpage không còn hoạt động.", "zernio_account_inactive", 400);
    }

    var mediaUrls = request.MediaUrls?
      .Where(url => !string.IsNullOrWhiteSpace(url))
      .Select(url => url.Trim())
      .ToList() ?? [];

    var body = new Dictionary<string, object?>
    {
      ["content"] = content,
      ["platforms"] = accounts.Select(account => new
      {
        platform = account.Platform,
        accountId = account.ZernioAccountId
      }).ToArray(),
      ["mediaUrls"] = mediaUrls,
      ["mediaItems"] = mediaUrls.Select(url => new
      {
        type = IsVideoUrl(url) ? "video" : "image",
        url
      }).ToArray(),
      ["publishNow"] = request.PublishNow || request.ScheduledFor is null
    };

    if (!request.PublishNow && request.ScheduledFor.HasValue)
    {
      body["scheduledFor"] = request.ScheduledFor.Value.ToUniversalTime().ToString("O");
      body["publishNow"] = false;
    }

    var response = await SendAsync<JsonElement>(HttpMethod.Post, "posts", null, body, cancellationToken);
    return MapPost(ExtractObject(response, "post", "data") ?? response);
  }

  public async Task<SocialPostListDto> GetPostsAsync(
    string? platform = null,
    Guid? accountId = null,
    string? profileId = null,
    int page = 1,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var query = new Dictionary<string, string>
    {
      ["page"] = Math.Max(page, 1).ToString(),
      ["limit"] = Math.Clamp(limit, 1, 100).ToString()
    };

    if (!string.IsNullOrWhiteSpace(platform))
    {
      query["platform"] = NormalizePlatform(platform);
    }

    if (!string.IsNullOrWhiteSpace(profileId))
    {
      query["profileId"] = profileId.Trim();
    }

    if (accountId.HasValue)
    {
      var account = await dbContext.SocialAccountConnections
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == accountId.Value, cancellationToken);
      if (account is not null)
      {
        query["accountId"] = account.ZernioAccountId;
      }
    }

    var response = await SendAsync<JsonElement>(HttpMethod.Get, "posts", query, null, cancellationToken);
    var posts = ExtractArray(response, "posts", "items", "data")
      .Select(MapPost)
      .ToList();

    return new SocialPostListDto(posts, Math.Max(page, 1), Math.Clamp(limit, 1, 100));
  }

  public async Task<SocialAnalyticsDto> GetAnalyticsAsync(
    string platform,
    DateOnly fromDate,
    DateOnly toDate,
    CancellationToken cancellationToken = default)
  {
    if (toDate < fromDate)
    {
      throw new ZernioApiException("Khoảng ngày thống kê không hợp lệ.", "zernio_validation_error", 400);
    }

    var normalizedPlatform = NormalizePlatform(platform);
    var query = new Dictionary<string, string>
    {
      ["platform"] = normalizedPlatform,
      ["fromDate"] = fromDate.ToString("yyyy-MM-dd"),
      ["toDate"] = toDate.ToString("yyyy-MM-dd")
    };

    var response = await SendAsync<JsonElement>(HttpMethod.Get, "analytics", query, null, cancellationToken);
    var data = ExtractObject(response, "data");
    var postsValue = ExtractValue(response, "posts")
      ?? (data.HasValue ? ExtractValue(data.Value, "posts") : null);
    var metricsSource = ExtractObject(response, "posts")
      ?? (data.HasValue ? ExtractObject(data.Value, "posts") : null)
      ?? data
      ?? response;

    return new SocialAnalyticsDto(
      normalizedPlatform,
      fromDate,
      toDate,
      postsValue.HasValue && postsValue.Value.ValueKind == JsonValueKind.Array
        ? SumPostAnalytics(postsValue.Value)
        : ReadAnalyticsMetrics(metricsSource));
  }

  public async Task<SocialMediaPresignDto> GetMediaPresignAsync(
    SocialMediaPresignRequest request,
    CancellationToken cancellationToken = default)
  {
    var body = new
    {
      filename = NormalizeRequired(request.FileName, "fileName"),
      contentType = NormalizeRequired(request.ContentType, "contentType"),
      fileName = request.FileName.Trim(),
      fileType = request.ContentType.Trim()
    };

    var response = await SendAsync<ZernioMediaPresignResponse>(HttpMethod.Post, "media/presign", null, body, cancellationToken);
    return new SocialMediaPresignDto(
      NormalizeRequired(response.UploadUrl, "uploadUrl"),
      NormalizeRequired(response.PublicUrl, "publicUrl"),
      ParseDate(response.Expires));
  }

  private async Task SyncAccountsAsync(string? platform, string? profileId, CancellationToken cancellationToken)
  {
    var normalizedPlatform = string.IsNullOrWhiteSpace(platform) ? null : NormalizePlatform(platform);
    var normalizedProfileId = string.IsNullOrWhiteSpace(profileId) ? null : profileId.Trim();
    var query = normalizedProfileId is null
      ? null
      : new Dictionary<string, string> { ["profileId"] = normalizedProfileId };
    var response = await SendAsync<JsonElement>(HttpMethod.Get, "accounts", query, null, cancellationToken);
    var accounts = ExtractArray(response, "accounts", "items", "data");

    foreach (var account in accounts)
    {
      var zernioAccountId = GetString(account, "_id", "id", "accountId");
      if (string.IsNullOrWhiteSpace(zernioAccountId)) continue;

      var accountPlatform = NormalizePlatform(GetString(account, "platform") ?? normalizedPlatform ?? "facebook");
      if (normalizedPlatform is not null && accountPlatform != normalizedPlatform) continue;

      var accountProfileId = GetString(account, "profileId", "profile_id") ?? normalizedProfileId ?? "default";
      if (normalizedProfileId is not null && accountProfileId != normalizedProfileId) continue;

      var existing = await dbContext.SocialAccountConnections
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(x => x.Provider == "zernio" && x.ZernioAccountId == zernioAccountId, cancellationToken);

      if (existing is null)
      {
        dbContext.SocialAccountConnections.Add(new SocialAccountConnection
        {
          Provider = "zernio",
          Platform = accountPlatform,
          ZernioProfileId = accountProfileId,
          ZernioAccountId = zernioAccountId,
          DisplayName = GetString(account, "displayName", "name", "username"),
          Username = GetString(account, "username", "handle"),
          AvatarUrl = GetString(account, "avatarUrl", "profilePicture", "image"),
          LastSyncedAt = DateTimeOffset.UtcNow,
          MetadataJson = SanitizeMetadata(account)
        });
      }
      else
      {
        existing.Platform = accountPlatform;
        existing.ZernioProfileId = accountProfileId;
        existing.DisplayName = GetString(account, "displayName", "name", "username") ?? existing.DisplayName;
        existing.Username = GetString(account, "username", "handle") ?? existing.Username;
        existing.AvatarUrl = GetString(account, "avatarUrl", "profilePicture", "image") ?? existing.AvatarUrl;
        existing.MetadataJson = SanitizeMetadata(account);
        existing.LastSyncedAt = DateTimeOffset.UtcNow;
        existing.IsActive = true;
        existing.IsDeleted = false;
        existing.DeletedAt = null;
        existing.UpdatedAt = DateTime.UtcNow;
      }
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<T> SendAsync<T>(
    HttpMethod method,
    string path,
    Dictionary<string, string>? query,
    object? body,
    CancellationToken cancellationToken)
  {
    using var client = httpClientFactory.CreateClient();
    using var request = new HttpRequestMessage(method, BuildUri(path, query));
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeApiKey());
    if (body is not null)
    {
      request.Content = JsonContent.Create(body, options: JsonOptions);
    }

    using var response = await client.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      await ThrowZernioExceptionAsync(response, cancellationToken);
    }

    try
    {
      return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
        ?? throw new ZernioApiException("Zernio trả về dữ liệu rỗng.", "zernio_empty_response", (int)response.StatusCode);
    }
    catch (JsonException ex)
    {
      throw new ZernioApiException("Không thể đọc phản hồi từ Zernio.", "zernio_invalid_response", (int)response.StatusCode, innerException: ex);
    }
  }

  private Uri BuildUri(string path, Dictionary<string, string>? query)
  {
    var baseUrl = NormalizeBaseUrl(settings.ApiUrl);
    var root = baseUrl.AbsolutePath.TrimEnd('/').EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase)
      ? baseUrl.ToString().TrimEnd('/')
      : baseUrl.ToString().TrimEnd('/') + "/api/v1";
    var builder = new UriBuilder($"{root}/{path.TrimStart('/')}");
    if (query is not null && query.Count > 0)
    {
      builder.Query = string.Join("&", query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
    }

    return builder.Uri;
  }

  private static async Task ThrowZernioExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    string? message = null;
    string? code = null;
    try
    {
      using var document = JsonDocument.Parse(body);
      var root = document.RootElement;
      message = GetString(root, "error", "message");
      code = GetString(root, "code", "reason");
    }
    catch
    {
      // Keep fallback message.
    }

    throw new ZernioApiException(
      $"Zernio API lỗi: {message ?? "Zernio từ chối yêu cầu."}",
      string.IsNullOrWhiteSpace(code) ? $"zernio_http_{(int)response.StatusCode}" : $"zernio_{code}",
      (int)response.StatusCode,
      response.Headers.RetryAfter?.Delta);
  }

  private static SocialAccountConnectionDto MapAccount(SocialAccountConnection account)
  {
    return new SocialAccountConnectionDto(
      account.Id,
      account.Provider,
      account.Platform,
      account.ZernioProfileId,
      account.ZernioAccountId,
      account.DisplayName,
      account.Username,
      account.AvatarUrl,
      account.LastSyncedAt,
      account.IsActive);
  }

  private static SocialPostDto MapPost(JsonElement post)
  {
    var platforms = ExtractArray(post, "platforms", "accounts")
      .Select(platform => new SocialPostPlatformDto(
        GetString(platform, "platform") ?? "facebook",
        GetString(platform, "accountId", "account_id", "id") ?? string.Empty,
        GetString(platform, "status"),
        GetString(platform, "platformPostUrl", "url", "permalinkUrl")))
      .ToList();

    return new SocialPostDto(
      GetString(post, "_id", "id") ?? string.Empty,
      GetString(post, "content", "message", "text"),
      GetString(post, "status"),
      ParseDate(GetString(post, "scheduledFor", "scheduled_for")),
      ParseDate(GetString(post, "publishedAt", "published_at")),
      GetString(post, "platformPostUrl", "url", "permalinkUrl"),
      platforms);
  }

  private static SocialAnalyticsMetricsDto SumPostAnalytics(JsonElement posts)
  {
    long impressions = 0;
    long likes = 0;
    long comments = 0;
    long shares = 0;
    long clicks = 0;
    long views = 0;

    foreach (var post in posts.EnumerateArray())
    {
      var analytics = ExtractObject(post, "analytics") ?? post;
      impressions += GetLong(analytics, "impressions");
      likes += GetLong(analytics, "likes");
      comments += GetLong(analytics, "comments");
      shares += GetLong(analytics, "shares");
      clicks += GetLong(analytics, "clicks");
      views += GetLong(analytics, "views");
    }

    return new SocialAnalyticsMetricsDto(impressions, likes, comments, shares, clicks, views);
  }

  private static SocialAnalyticsMetricsDto ReadAnalyticsMetrics(JsonElement metrics)
  {
    return new SocialAnalyticsMetricsDto(
      GetLong(metrics, "impressions"),
      GetLong(metrics, "likes"),
      GetLong(metrics, "comments"),
      GetLong(metrics, "shares"),
      GetLong(metrics, "clicks"),
      GetLong(metrics, "views"));
  }

  private static JsonElement? ExtractValue(JsonElement root, params string[] names)
  {
    if (root.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
    {
      if (root.TryGetProperty(name, out var value)) return value;
    }

    return null;
  }

  private static JsonElement? ExtractObject(JsonElement root, params string[] names)
  {
    if (root.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
    {
      if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object)
      {
        return value;
      }
    }

    return null;
  }

  private static IReadOnlyList<JsonElement> ExtractArray(JsonElement root, params string[] names)
  {
    if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray().ToList();
    if (root.ValueKind != JsonValueKind.Object) return [];

    foreach (var name in names)
    {
      if (!root.TryGetProperty(name, out var value)) continue;
      if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().ToList();
      if (value.ValueKind == JsonValueKind.Object)
      {
        var nested = ExtractArray(value, names);
        if (nested.Count > 0) return nested;
      }
    }

    return [];
  }

  private static string? GetString(JsonElement element, params string[] names)
  {
    if (element.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
    {
      if (!element.TryGetProperty(name, out var value)) continue;
      if (value.ValueKind == JsonValueKind.String) return value.GetString();
      if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False) return value.ToString();
    }

    return null;
  }

  private static long GetLong(JsonElement element, params string[] names)
  {
    if (element.ValueKind != JsonValueKind.Object) return 0;
    foreach (var name in names)
    {
      if (!element.TryGetProperty(name, out var value)) continue;
      if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)) return number;
      if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var parsed)) return parsed;
    }

    return 0;
  }

  private static string? SanitizeMetadata(JsonElement account)
  {
    if (account.ValueKind != JsonValueKind.Object) return null;
    var metadata = new Dictionary<string, string?>
    {
      ["rawId"] = GetString(account, "_id", "id", "accountId"),
      ["platform"] = GetString(account, "platform"),
      ["username"] = GetString(account, "username", "handle"),
      ["displayName"] = GetString(account, "displayName", "name")
    };
    return JsonSerializer.Serialize(metadata, JsonOptions);
  }

  private string NormalizeApiKey()
  {
    var apiKey = NormalizeRequired(settings.ApiKey, "Zernio:ApiKey");
    if (apiKey == "CHANGE_ME" || apiKey == "YOUR_ZERNIO_API_KEY")
    {
      logger.LogWarning("Zernio API key placeholder is still configured.");
    }

    return apiKey;
  }

  private static Uri NormalizeBaseUrl(string value)
  {
    var url = NormalizeRequired(value, "Zernio:ApiUrl");
    if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
    {
      throw new ZernioApiException("Zernio:ApiUrl không hợp lệ.", "zernio_invalid_config", 500);
    }

    return parsed;
  }

  private static string NormalizeAbsoluteUri(string? value, string field)
  {
    var uri = NormalizeRequired(value, field);
    if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
    {
      throw new ZernioApiException($"{field} không hợp lệ.", "zernio_validation_error", 400);
    }

    return parsed.ToString();
  }

  private static string NormalizePlatform(string? value)
  {
    var platform = NormalizeRequired(value, "platform").ToLowerInvariant();
    return platform == "fb" ? "facebook" : platform;
  }

  private static string NormalizeRequired(string? value, string field)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ZernioApiException($"Thiếu thông tin {field}.", "zernio_validation_error", 400);
    }

    return value.Trim();
  }

  private static bool IsVideoUrl(string url)
  {
    return url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
      || url.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
      || url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
  }

  private static DateTimeOffset? ParseDate(string? value)
  {
    return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
  }

  private sealed record ZernioConnectUrlResponse(
    [property: JsonPropertyName("authUrl")] string AuthUrl,
    [property: JsonPropertyName("state")] string? State);

  private sealed record ZernioMediaPresignResponse(
    [property: JsonPropertyName("uploadUrl")] string UploadUrl,
    [property: JsonPropertyName("publicUrl")] string PublicUrl,
    [property: JsonPropertyName("expires")] string? Expires);
}
