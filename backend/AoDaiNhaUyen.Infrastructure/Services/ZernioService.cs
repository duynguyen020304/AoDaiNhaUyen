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
    return MapPost(ExtractObject(response, "post", "existingPost", "data") ?? response);
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

  public async Task<SocialPostDto> GetPostAsync(
    string postId,
    CancellationToken cancellationToken = default)
  {
    var normalizedPostId = NormalizeRequired(postId, "postId");
    var response = await SendAsync<JsonElement>(HttpMethod.Get, $"posts/{Uri.EscapeDataString(normalizedPostId)}", null, null, cancellationToken);
    return MapPost(ExtractObject(response, "post", "existingPost", "data") ?? response);
  }

  public async Task<SocialPostDto> UpdatePostAsync(
    string postId,
    UpdateSocialPostRequest request,
    CancellationToken cancellationToken = default)
  {
    var normalizedPostId = NormalizeRequired(postId, "postId");
    var body = new Dictionary<string, object?>();

    if (request.Content is not null)
    {
      body["content"] = request.Content.Trim();
    }

    if (request.PublishNow.HasValue)
    {
      body["publishNow"] = request.PublishNow.Value;
    }

    if (request.ScheduledFor.HasValue)
    {
      body["scheduledFor"] = request.ScheduledFor.Value.ToUniversalTime().ToString("O");
      body["publishNow"] = false;
    }

    if (request.MediaUrls is not null)
    {
      var mediaUrls = request.MediaUrls
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Select(url => url.Trim())
        .ToList();
      body["mediaUrls"] = mediaUrls;
      body["mediaItems"] = mediaUrls.Select(url => new
      {
        type = IsVideoUrl(url) ? "video" : "image",
        url
      }).ToArray();
    }

    if (request.AccountIds is { Count: > 0 })
    {
      var accounts = await dbContext.SocialAccountConnections
        .AsNoTracking()
        .Where(x => request.AccountIds.Contains(x.Id) && x.IsActive)
        .ToListAsync(cancellationToken);

      if (accounts.Count != request.AccountIds.Count)
      {
        throw new ZernioApiException("Một hoặc nhiều fanpage không còn hoạt động.", "zernio_account_inactive", 400);
      }

      body["platforms"] = accounts.Select(account => new
      {
        platform = account.Platform,
        accountId = account.ZernioAccountId
      }).ToArray();
    }

    var response = await SendAsync<JsonElement>(HttpMethod.Put, $"posts/{Uri.EscapeDataString(normalizedPostId)}", null, body, cancellationToken);
    return MapPost(ExtractObject(response, "post", "data") ?? response);
  }

  public async Task DeletePostAsync(
    string postId,
    CancellationToken cancellationToken = default)
  {
    var normalizedPostId = NormalizeRequired(postId, "postId");
    await SendAsync<JsonElement>(HttpMethod.Delete, $"posts/{Uri.EscapeDataString(normalizedPostId)}", null, null, cancellationToken);
  }

  public async Task<SocialPostActionResultDto> UnpublishPostAsync(
    string postId,
    UnpublishSocialPostRequest request,
    CancellationToken cancellationToken = default)
  {
    var normalizedPostId = NormalizeRequired(postId, "postId");
    var platform = NormalizePlatform(request.Platform);
    var response = await SendAsync<JsonElement>(
      HttpMethod.Post,
      $"posts/{Uri.EscapeDataString(normalizedPostId)}/unpublish",
      null,
      new { platform },
      cancellationToken);

    return new SocialPostActionResultDto(
      GetBool(response, "success"),
      GetString(response, "message"));
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

  public async Task<SocialCommentedPostListDto> GetCommentedPostsAsync(
    string? platform = "facebook",
    string? accountId = null,
    string? profileId = null,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var query = BuildInboxQuery(platform, accountId, profileId, cursor, limit);
    var response = await SendAsync<JsonElement>(HttpMethod.Get, "inbox/comments", query, null, cancellationToken);
    return new SocialCommentedPostListDto(
      ExtractArray(response, "data", "items", "posts").Select(MapCommentedPost).ToList(),
      GetPaginationCursor(response),
      GetPaginationHasMore(response));
  }

  public async Task<SocialCommentListDto> GetCommentsAsync(
    string postId,
    string accountId,
    string? cursor = null,
    int limit = 50,
    CancellationToken cancellationToken = default)
  {
    var query = new Dictionary<string, string>
    {
      ["accountId"] = NormalizeRequired(accountId, "accountId"),
      ["limit"] = Math.Clamp(limit, 1, 100).ToString()
    };
    if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor.Trim();

    var response = await SendAsync<JsonElement>(HttpMethod.Get, $"inbox/comments/{Uri.EscapeDataString(NormalizeRequired(postId, "postId"))}", query, null, cancellationToken);
    return new SocialCommentListDto(
      ExtractArray(response, "comments", "data", "items").Select(MapComment).ToList(),
      GetPaginationCursor(response),
      GetPaginationHasMore(response));
  }

  public async Task<SocialActionResultDto> ReplyToCommentAsync(
    string postId,
    CreateSocialCommentReplyRequest request,
    CancellationToken cancellationToken = default)
  {
    var body = new Dictionary<string, object?>
    {
      ["accountId"] = NormalizeRequired(request.AccountId, "accountId"),
      ["message"] = NormalizeRequired(request.Message, "message")
    };
    if (!string.IsNullOrWhiteSpace(request.CommentId)) body["commentId"] = request.CommentId.Trim();

    var response = await SendAsync<JsonElement>(HttpMethod.Post, $"inbox/comments/{Uri.EscapeDataString(NormalizeRequired(postId, "postId"))}", null, body, cancellationToken);
    var data = ExtractObject(response, "data") ?? response;
    return new SocialActionResultDto(GetBool(response, "success") || GetBool(data, "success"), GetString(response, "message") ?? GetString(data, "message"), GetString(data, "commentId", "id"));
  }

  public async Task<SocialActionResultDto> DeleteCommentAsync(
    string postId,
    string accountId,
    string commentId,
    CancellationToken cancellationToken = default)
  {
    var query = new Dictionary<string, string>
    {
      ["accountId"] = NormalizeRequired(accountId, "accountId"),
      ["commentId"] = NormalizeRequired(commentId, "commentId")
    };
    var response = await SendAsync<JsonElement>(HttpMethod.Delete, $"inbox/comments/{Uri.EscapeDataString(NormalizeRequired(postId, "postId"))}", query, null, cancellationToken);
    return new SocialActionResultDto(GetBool(response, "success"), GetString(response, "message"), commentId);
  }

  public async Task<SocialActionResultDto> ToggleCommentHiddenAsync(
    string postId,
    string accountId,
    string commentId,
    bool isHidden,
    CancellationToken cancellationToken = default)
  {
    var normalizedPostId = Uri.EscapeDataString(NormalizeRequired(postId, "postId"));
    var normalizedCommentId = Uri.EscapeDataString(NormalizeRequired(commentId, "commentId"));
    var path = isHidden ? $"inbox/comments/{normalizedPostId}/{normalizedCommentId}/hide" : $"inbox/comments/{normalizedPostId}/{normalizedCommentId}/hide";
    var method = isHidden ? HttpMethod.Post : HttpMethod.Delete;
    var body = isHidden ? new { accountId = NormalizeRequired(accountId, "accountId") } : null;
    var query = isHidden ? null : new Dictionary<string, string> { ["accountId"] = NormalizeRequired(accountId, "accountId") };
    var response = await SendAsync<JsonElement>(method, path, query, body, cancellationToken);
    return new SocialActionResultDto(GetBool(response, "success") || true, isHidden ? "Đã ẩn bình luận." : "Đã hiện bình luận.", commentId);
  }

  public async Task<SocialConversationListDto> GetConversationsAsync(
    string? platform = "facebook",
    string? accountId = null,
    string? profileId = null,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var query = BuildInboxQuery(platform, accountId, profileId, cursor, limit);
    var response = await SendAsync<JsonElement>(HttpMethod.Get, "inbox/conversations", query, null, cancellationToken);
    return new SocialConversationListDto(
      ExtractArray(response, "data", "items", "conversations").Select(MapConversation).ToList(),
      GetPaginationCursor(response),
      GetPaginationHasMore(response));
  }

  public async Task<SocialMessageListDto> GetConversationMessagesAsync(
    string conversationId,
    string accountId,
    string? cursor = null,
    int limit = 50,
    CancellationToken cancellationToken = default)
  {
    var query = new Dictionary<string, string>
    {
      ["accountId"] = NormalizeRequired(accountId, "accountId"),
      ["limit"] = Math.Clamp(limit, 1, 100).ToString(),
      ["sortOrder"] = "asc"
    };
    if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor.Trim();

    var response = await SendAsync<JsonElement>(HttpMethod.Get, $"inbox/conversations/{Uri.EscapeDataString(NormalizeRequired(conversationId, "conversationId"))}/messages", query, null, cancellationToken);
    return new SocialMessageListDto(
      ExtractArray(response, "messages", "data", "items").Select(MapMessage).ToList(),
      GetPaginationCursor(response),
      GetPaginationHasMore(response));
  }

  public async Task<SocialActionResultDto> SendMessageAsync(
    string conversationId,
    SendSocialMessageRequest request,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
    {
      throw new ZernioApiException("Tin nhắn cần có nội dung hoặc media.", "zernio_message_empty", 400);
    }

    var body = new Dictionary<string, object?>
    {
      ["accountId"] = NormalizeRequired(request.AccountId, "accountId")
    };
    if (!string.IsNullOrWhiteSpace(request.Message)) body["message"] = request.Message.Trim();
    if (!string.IsNullOrWhiteSpace(request.AttachmentUrl)) body["attachmentUrl"] = NormalizeAbsoluteUri(request.AttachmentUrl, "attachmentUrl");
    if (!string.IsNullOrWhiteSpace(request.AttachmentType)) body["attachmentType"] = request.AttachmentType.Trim();
    var response = await SendAsync<JsonElement>(HttpMethod.Post, $"inbox/conversations/{Uri.EscapeDataString(NormalizeRequired(conversationId, "conversationId"))}/messages", null, body, cancellationToken);
    var data = ExtractObject(response, "data") ?? response;
    return new SocialActionResultDto(GetBool(response, "success") || GetBool(data, "success"), GetString(response, "message") ?? GetString(data, "message"), GetString(data, "messageId", "id"));
  }

  public async Task<SocialActionResultDto> MarkConversationReadAsync(
    string conversationId,
    string accountId,
    CancellationToken cancellationToken = default)
  {
    var body = new { accountId = NormalizeRequired(accountId, "accountId") };
    var response = await SendAsync<JsonElement>(HttpMethod.Post, $"inbox/conversations/{Uri.EscapeDataString(NormalizeRequired(conversationId, "conversationId"))}/read", null, body, cancellationToken);
    return new SocialActionResultDto(GetBool(response, "success") || true, "Đã đánh dấu đã đọc.", conversationId);
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

  private static Dictionary<string, string> BuildInboxQuery(string? platform, string? accountId, string? profileId, string? cursor, int limit)
  {
    var query = new Dictionary<string, string>
    {
      ["platform"] = string.IsNullOrWhiteSpace(platform) ? "facebook" : NormalizePlatform(platform),
      ["limit"] = Math.Clamp(limit, 1, 100).ToString(),
      ["sortOrder"] = "desc"
    };
    if (!string.IsNullOrWhiteSpace(accountId)) query["accountId"] = accountId.Trim();
    if (!string.IsNullOrWhiteSpace(profileId)) query["profileId"] = profileId.Trim();
    if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor.Trim();
    return query;
  }

  private static string? GetPaginationCursor(JsonElement response)
  {
    var pagination = ExtractObject(response, "pagination");
    return pagination.HasValue ? GetString(pagination.Value, "nextCursor", "cursor") : null;
  }

  private static bool GetPaginationHasMore(JsonElement response)
  {
    var pagination = ExtractObject(response, "pagination");
    return pagination.HasValue && GetBool(pagination.Value, "hasMore");
  }

  private static SocialCommentedPostDto MapCommentedPost(JsonElement post)
  {
    return new SocialCommentedPostDto(
      GetString(post, "id") ?? string.Empty,
      GetString(post, "platform") ?? "facebook",
      GetString(post, "accountId", "account_id") ?? string.Empty,
      GetString(post, "accountUsername", "account_username"),
      GetString(post, "content", "message", "text"),
      GetString(post, "picture", "image", "thumbnail"),
      GetString(post, "permalink", "url", "link"),
      ParseDate(GetString(post, "createdTime", "createdAt", "created_time")),
      (int)GetLong(post, "commentCount", "comments"),
      (int)GetLong(post, "likeCount", "likes"));
  }

  private static SocialCommentDto MapComment(JsonElement comment)
  {
    return new SocialCommentDto(
      GetString(comment, "id", "commentId") ?? string.Empty,
      GetString(comment, "parentId", "parentCommentId"),
      MapCommentAuthor(ExtractObject(comment, "from", "author")),
      GetString(comment, "message", "text", "content"),
      ParseDate(GetString(comment, "createdTime", "createdAt", "created_time")),
      (int)GetLong(comment, "likeCount", "likes"),
      (int)GetLong(comment, "replyCount", "commentCount", "repliesCount"),
      GetString(comment, "platform"),
      GetString(comment, "url", "permalink"),
      GetBool(comment, "canReply"),
      GetBool(comment, "canDelete", "canRemove"),
      GetBool(comment, "canHide"),
      GetBool(comment, "isHidden"),
      ExtractArray(comment, "replies", "comments").Select(MapComment).ToList());
  }

  private static SocialCommentAuthorDto? MapCommentAuthor(JsonElement? author)
  {
    return author.HasValue
      ? new SocialCommentAuthorDto(
        GetString(author.Value, "id"),
        GetString(author.Value, "name"),
        GetString(author.Value, "username"),
        GetString(author.Value, "picture", "avatarUrl", "profilePicture"),
        GetBool(author.Value, "isOwner"))
      : null;
  }

  private static SocialConversationDto MapConversation(JsonElement conversation)
  {
    return new SocialConversationDto(
      GetString(conversation, "id", "conversationId") ?? string.Empty,
      GetString(conversation, "platform") ?? "facebook",
      GetString(conversation, "accountId", "account_id") ?? string.Empty,
      GetString(conversation, "accountUsername", "account_username"),
      GetString(conversation, "participantId"),
      GetString(conversation, "participantName"),
      GetString(conversation, "participantPicture", "participantAvatar"),
      GetString(conversation, "lastMessage", "snippet"),
      ParseDate(GetString(conversation, "updatedTime", "updatedAt", "updated_time")),
      GetString(conversation, "status"),
      (int?)GetLong(conversation, "unreadCount"),
      GetString(conversation, "url", "link"));
  }

  private static SocialMessageDto MapMessage(JsonElement message)
  {
    return new SocialMessageDto(
      GetString(message, "id", "messageId") ?? string.Empty,
      GetString(message, "conversationId") ?? string.Empty,
      GetString(message, "accountId") ?? string.Empty,
      GetString(message, "platform"),
      GetString(message, "message", "text"),
      GetString(message, "senderId"),
      GetString(message, "senderName"),
      GetString(message, "direction") ?? "incoming",
      ParseDate(GetString(message, "createdAt", "sentAt", "createdTime")),
      ExtractArray(message, "attachments", "media").Select(MapMessageAttachment).ToList());
  }

  private static SocialMessageAttachmentDto MapMessageAttachment(JsonElement attachment)
  {
    return new SocialMessageAttachmentDto(
      GetString(attachment, "id"),
      GetString(attachment, "type"),
      GetString(attachment, "url"),
      GetString(attachment, "filename", "fileName", "name"),
      GetString(attachment, "previewUrl", "thumbnail"));
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
        GetString(platform, "platformPostId", "postId", "platform_post_id"),
        ParseDate(GetString(platform, "publishedAt", "published_at")),
        GetString(platform, "platformPostUrl", "url", "permalinkUrl", "permalink", "link"),
        GetString(platform, "errorMessage", "error_message", "error")))
      .ToList();

    var publishedAt = ParseDate(GetString(post, "publishedAt", "published_at"))
      ?? platforms
        .Where(platform => platform.PublishedAt.HasValue)
        .OrderByDescending(platform => platform.PublishedAt)
        .Select(platform => platform.PublishedAt)
        .FirstOrDefault();
    var platformPostUrl = GetString(post, "platformPostUrl", "url", "permalinkUrl", "permalink", "link")
      ?? platforms.FirstOrDefault(platform => !string.IsNullOrWhiteSpace(platform.PlatformPostUrl))?.PlatformPostUrl;

    return new SocialPostDto(
      GetString(post, "_id", "id") ?? string.Empty,
      GetString(post, "content", "message", "text"),
      GetString(post, "status"),
      ParseDate(GetString(post, "scheduledFor", "scheduled_for")),
      publishedAt,
      platformPostUrl,
      platforms,
      MapMediaItems(post));
  }

  private static IReadOnlyList<SocialPostMediaDto> MapMediaItems(JsonElement post)
  {
    return ExtractArray(post, "mediaItems", "media", "attachments", "mediaUrls", "images")
      .Select(media => media.ValueKind == JsonValueKind.String
        ? new SocialPostMediaDto(IsVideoUrl(media.GetString() ?? string.Empty) ? "video" : "image", media.GetString() ?? string.Empty)
        : new SocialPostMediaDto(
          GetString(media, "type", "mediaType", "kind"),
          GetString(media, "url", "mediaUrl", "src", "publicUrl") ?? string.Empty))
      .Where(media => !string.IsNullOrWhiteSpace(media.Url))
      .ToList();
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

  private static bool GetBool(JsonElement element, params string[] names)
  {
    if (element.ValueKind != JsonValueKind.Object) return false;
    foreach (var name in names)
    {
      if (!element.TryGetProperty(name, out var value)) continue;
      if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return value.GetBoolean();
      if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed)) return parsed;
    }

    return false;
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
