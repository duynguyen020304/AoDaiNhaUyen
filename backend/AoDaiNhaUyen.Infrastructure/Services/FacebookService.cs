using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Facebook;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class FacebookService(
  IHttpClientFactory httpClientFactory,
  IOptions<FacebookApiSettings> facebookApiSettings,
  IDataProtectionProvider dataProtectionProvider,
  AppDbContext dbContext,
  ISocialService socialService,
  ILogger<FacebookService> logger) : IFacebookService
{
  private const string PostFields = "id,message,created_time,updated_time,permalink_url,full_picture,is_published,scheduled_publish_time,status_type,type";
  private const string CommentFields = "id,parent,from{id,name,picture},message,created_time,like_count,comment_count,can_comment,can_remove,is_hidden,comments.limit(25){id,parent,from{id,name,picture},message,created_time,like_count,comment_count,can_comment,can_remove,is_hidden}";
  private const string ConversationFields = "id,updated_time,message_count,unread_count,snippet,link,participants{id,name,email}";
  private const string MessageFields = "id,created_time,from{id,name},to{id,name},message,attachments";
  private const string OAuthScopes = "pages_show_list,pages_read_engagement,pages_manage_posts,pages_manage_metadata,pages_messaging";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly FacebookApiSettings facebookApiSettings = facebookApiSettings.Value;
  private bool UseMockData => facebookApiSettings.UseMockData;
  private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("AoDaiNhaUyen.Facebook.PageAccessToken.v1");
  private readonly IDataProtector oauthConnectTokenProtector = dataProtectionProvider.CreateProtector("AoDaiNhaUyen.Facebook.OAuthConnectToken.v1");

  public async Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken = default)
  {
    if (UseMockData)
    {
      var realConnections = await dbContext.FacebookPageConnections
        .AsNoTracking()
        .OrderBy(x => x.PageName ?? x.PageId)
        .Select(x => new FacebookConnectionDto(
          x.PageId,
          x.PageName,
          x.TokenLast4,
          x.ExpiresAt,
          x.LastValidatedAt,
          x.IsActive))
        .ToListAsync(cancellationToken);
      return realConnections.Count > 0 ? realConnections : MockConnections;
    }

    var accounts = await socialService.GetAccountsAsync("facebook", sync: true, cancellationToken: cancellationToken);
    return accounts
      .Where(account => account.IsActive)
      .OrderBy(account => account.DisplayName ?? account.Username ?? account.ZernioAccountId)
      .Select(ToFacebookConnection)
      .ToList();
  }

  private static readonly IReadOnlyList<FacebookConnectionDto> MockConnections =
  [
    new("mock_page_aodai_nha_uyen", "MOCK - Áo Dài Nhã Uyên", "MOCK", null, DateTimeOffset.UtcNow, true)
  ];

  private static bool IsMockPage(string pageId) => pageId.StartsWith("mock_page_", StringComparison.OrdinalIgnoreCase);

  private static FacebookPostListDto MockPosts(string? cursor, int limit)
  {
    var take = Math.Clamp(limit, 1, 50);
    var firstPage = string.IsNullOrWhiteSpace(cursor);
    var posts = Enumerable.Range(firstPage ? 1 : 1 + take, take)
      .Select(i => new FacebookPostDto(
        $"mock_post_{i}",
        $"MOCK: Bài đăng mẫu #{i} về áo dài Nhã Uyên, dùng để test workflow Facebook an toàn.",
        DateTimeOffset.UtcNow.AddDays(-i),
        DateTimeOffset.UtcNow.AddDays(-i),
        $"https://facebook.test/mock_page_aodai_nha_uyen/posts/mock_post_{i}",
        null,
        true,
        null,
        "published",
        "status"))
      .ToList();

    return new FacebookPostListDto(posts, null, firstPage ? "mock_cursor_2" : null, firstPage ? "https://facebook.test/mock_next" : null);
  }

  private static FacebookPostCommentListDto MockComments(string postId, string? after, int limit)
  {
    var take = Math.Clamp(limit, 1, 50);
    var firstPage = string.IsNullOrWhiteSpace(after);
    var comments = Enumerable.Range(firstPage ? 1 : 1 + take, take)
      .Select(i => new FacebookCommentDto(
        $"mock_comment_{i}",
        postId,
        null,
        new FacebookCommentAuthorDto($"mock_user_{i}", $"Khách mẫu {i}", null),
        i % 2 == 0 ? "Mẫu áo dài này còn size M không ạ?" : "Áo đẹp quá, shop tư vấn giúp mình nhé.",
        DateTimeOffset.UtcNow.AddHours(-i),
        i,
        0,
        true,
        true,
        false,
        false,
        []))
      .ToList();

    return new FacebookPostCommentListDto(comments, null, firstPage ? "mock_comment_cursor_2" : null, firstPage ? "https://facebook.test/mock_comments_next" : null);
  }


  private static FacebookConversationListDto MockConversations(string pageId, string? after, int limit)
  {
    var take = Math.Clamp(limit, 1, 50);
    var firstPage = string.IsNullOrWhiteSpace(after);
    var conversations = Enumerable.Range(firstPage ? 1 : 1 + take, take)
      .Select(i => new FacebookConversationDto(
        $"mock_conversation_{i}",
        pageId,
        $"mock_customer_{i}",
        $"Khách Messenger {i}",
        null,
        i % 2 == 0 ? "Shop tư vấn giúp mình mẫu áo dài cưới." : "Mình muốn hỏi còn lịch fitting tuần này không?",
        DateTimeOffset.UtcNow.AddMinutes(-15 * i),
        i % 3 == 0 ? 1 : 0,
        3 + i,
        $"https://facebook.test/{pageId}/inbox/mock_conversation_{i}",
        [
          new FacebookParticipantDto(pageId, "MOCK - Áo Dài Nhã Uyên", null, true),
          new FacebookParticipantDto($"mock_customer_{i}", $"Khách Messenger {i}", null, false)
        ]))
      .ToList();

    return new FacebookConversationListDto(conversations, null, firstPage ? "mock_conversation_cursor_2" : null, null);
  }

  private static FacebookMessageListDto MockMessages(string pageId, string conversationId, string? before, int limit)
  {
    var take = Math.Clamp(limit, 1, 100);
    var firstPage = string.IsNullOrWhiteSpace(before);
    var messages = Enumerable.Range(firstPage ? 1 : 1 + take, take)
      .Select(i => new FacebookMessageDto(
        $"mock_message_{i}",
        conversationId,
        i % 2 == 0 ? pageId : $"mock_customer_{i}",
        i % 2 == 0 ? "MOCK - Áo Dài Nhã Uyên" : $"Khách Messenger {i}",
        i % 2 == 0,
        i % 2 == 0 ? "Dạ shop có thể tư vấn mẫu phù hợp ạ." : "Mình muốn xem mẫu áo dài mới nhất.",
        DateTimeOffset.UtcNow.AddMinutes(-10 * i),
        []))
      .ToList();

    return new FacebookMessageListDto(messages, null, firstPage ? "mock_message_cursor_2" : null, null);
  }

  public async Task<FacebookConnectionDto> ConnectPageAsync(
    ConnectFacebookPageRequest request,
    CancellationToken cancellationToken = default)
  {
    var pageId = NormalizeRequired(request.PageId, "pageId");
    var token = NormalizeRequired(request.PageAccessToken, "pageAccessToken");
    var pageInfo = await GetPageInfoWithTokenAsync(pageId, token, cancellationToken);
    return await UpsertPageConnectionAsync(pageId, request.PageName, pageInfo.Name, token, null, cancellationToken);
  }

  public Task<FacebookOAuthUrlDto> GetOAuthUrlAsync(
    string redirectUri,
    string state,
    CancellationToken cancellationToken = default)
  {
    _ = cancellationToken;
    redirectUri = NormalizeAbsoluteUri(redirectUri, "redirectUri");
    state = NormalizeRequired(state, "state");

    var query = new Dictionary<string, string>
    {
      ["client_id"] = NormalizeRequired(facebookApiSettings.AppId, "FacebookApi:AppId"),
      ["redirect_uri"] = redirectUri,
      ["state"] = state,
      ["scope"] = OAuthScopes,
      ["response_type"] = "code"
    };

    return Task.FromResult(new FacebookOAuthUrlDto(BuildDialogUri("oauth", query).ToString()));
  }

  public async Task<IReadOnlyList<FacebookOAuthPageDto>> GetOAuthPagesAsync(
    FacebookOAuthPagesRequest request,
    CancellationToken cancellationToken = default)
  {
    var redirectUri = NormalizeAbsoluteUri(request.RedirectUri, "redirectUri");
    var code = NormalizeRequired(request.Code, "code");
    var appAccessToken = $"{NormalizeRequired(facebookApiSettings.AppId, "FacebookApi:AppId")}|{NormalizeRequired(facebookApiSettings.AppSecret, "FacebookApi:AppSecret")}";
    var shortLivedToken = await ExchangeCodeForUserTokenAsync(code, redirectUri, cancellationToken);
    var longLivedToken = await ExchangeForLongLivedUserTokenAsync(shortLivedToken, cancellationToken);
    var pages = await SendGetAsync<FacebookListResponse<FacebookOAuthAccountResponse>>(
      "me/accounts",
      longLivedToken,
      new Dictionary<string, string> { ["fields"] = "id,name,category,access_token,tasks" },
      cancellationToken);

    var result = new List<FacebookOAuthPageDto>();
    foreach (var page in pages.Data)
    {
      if (string.IsNullOrWhiteSpace(page.AccessToken)) continue;

      var (pageToken, tokenExpiresAt) = await ValidatePageTokenAsync(page.AccessToken, appAccessToken, cancellationToken);
      result.Add(new FacebookOAuthPageDto(
        page.Id,
        page.Name,
        page.Category,
        page.Tasks ?? Array.Empty<string>(),
        ProtectOAuthConnectToken(page.Id, page.Name, pageToken, tokenExpiresAt)));
    }

    return result;
  }

  public async Task<FacebookConnectionDto> ConnectOAuthPageAsync(
    ConnectFacebookOAuthPageRequest request,
    CancellationToken cancellationToken = default)
  {
    var pageId = NormalizeRequired(request.PageId, "pageId");
    var payload = UnprotectOAuthConnectToken(request.ConnectToken);
    if (!string.Equals(payload.PageId, pageId, StringComparison.Ordinal))
    {
      throw new FacebookApiException("Token không khớp fanpage đã chọn.", "facebook_oauth_token_mismatch", 400);
    }

    return await UpsertPageConnectionAsync(
      payload.PageId,
      payload.PageName,
      payload.PageName,
      payload.PageAccessToken,
      payload.TokenExpiresAt,
      cancellationToken);
  }

  public async Task DisconnectPageAsync(string pageId, CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    var connection = await dbContext.FacebookPageConnections.FirstOrDefaultAsync(x => x.PageId == pageId, cancellationToken);
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

  public async Task<FacebookPageInfoDto> GetPageInfoAsync(string pageId, CancellationToken cancellationToken = default)
  {
    var token = await GetPageTokenAsync(pageId, cancellationToken);
    return await GetPageInfoWithTokenAsync(pageId, token, cancellationToken);
  }

  public async Task<FacebookPublishResultDto> PublishPostAsync(
    string pageId,
    CreateFacebookPostRequest request,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    if (UseMockData && IsMockPage(pageId))
    {
      var id = $"mock_post_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
      return new FacebookPublishResultDto(id, id, $"https://facebook.test/{pageId}/posts/{id}");
    }

    var mediaUrls = request.MediaUrls?
      .Where(url => !string.IsNullOrWhiteSpace(url))
      .Select(url => url.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList() ?? [];

    if (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrWhiteSpace(request.Link) && mediaUrls.Count == 0)
    {
      throw new FacebookApiException("Bài viết cần có nội dung, liên kết hoặc URL ảnh public.", "facebook_post_empty", 400);
    }

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var content = BuildSocialPostContent(request.Message, request.Link);
    var publishNow = request.Published && !ShouldSchedule(request.ScheduledPublishTime);
    var post = await socialService.CreatePostAsync(
      new CreateSocialPostRequest(content, [account.Id], publishNow, request.ScheduledPublishTime, mediaUrls),
      cancellationToken);

    return new FacebookPublishResultDto(post.Id, post.Platforms.FirstOrDefault()?.PlatformPostId, post.PlatformPostUrl);
  }

  public async Task<FacebookPublishResultDto> PublishPhotoAsync(
    string pageId,
    Stream imageStream,
    string fileName,
    string contentType,
    string? caption,
    DateTimeOffset? scheduledPublishTime = null,
    bool published = true,
    CancellationToken cancellationToken = default)
  {
    return await PublishMediaAsync(pageId, "photos", "source", imageStream, fileName, contentType, caption, scheduledPublishTime, published, cancellationToken);
  }

  public async Task<FacebookPublishResultDto> PublishVideoAsync(
    string pageId,
    Stream videoStream,
    string fileName,
    string contentType,
    string? description,
    DateTimeOffset? scheduledPublishTime = null,
    bool published = true,
    CancellationToken cancellationToken = default)
  {
    return await PublishMediaAsync(pageId, "videos", "source", videoStream, fileName, contentType, description, scheduledPublishTime, published, cancellationToken);
  }

  public async Task<FacebookPostListDto> GetPostsAsync(
    string pageId,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    if (UseMockData && IsMockPage(pageId)) return MockPosts(cursor, limit);

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var page = ParseCursorPage(cursor);
    var normalizedLimit = Math.Clamp(limit, 1, 100);
    var posts = await socialService.GetPostsAsync("facebook", account.Id, account.ZernioProfileId, page, normalizedLimit, cancellationToken);
    var nextCursor = posts.Items.Count >= normalizedLimit ? (page + 1).ToString() : null;

    return new FacebookPostListDto(
      posts.Items.Select(MapPost).ToList(),
      page > 1 ? (page - 1).ToString() : null,
      nextCursor,
      null);
  }

  public async Task<FacebookPostDto> GetPostAsync(string postId, CancellationToken cancellationToken = default)
  {
    postId = NormalizeRequired(postId, "postId");
    var token = await GetAnyActiveTokenAsync(cancellationToken);
    var response = await SendGetAsync<FacebookPostResponse>(postId, token, new Dictionary<string, string> { ["fields"] = PostFields }, cancellationToken);
    return MapPost(response);
  }

  public async Task<FacebookPostDto> UpdatePostAsync(
    string postId,
    UpdateFacebookPostRequest request,
    CancellationToken cancellationToken = default)
  {
    postId = NormalizeRequired(postId, "postId");
    var message = NormalizeRequired(request.Message, "message");
    var token = await GetAnyActiveTokenAsync(cancellationToken);
    await SendFormAsync<FacebookSuccessResponse>(HttpMethod.Post, postId, token, new Dictionary<string, string> { ["message"] = message }, cancellationToken);
    return await GetPostAsync(postId, cancellationToken);
  }

  public async Task<FacebookDeleteResultDto> DeletePostAsync(string postId, CancellationToken cancellationToken = default)
  {
    postId = NormalizeRequired(postId, "postId");
    if (UseMockData && postId.StartsWith("mock_", StringComparison.OrdinalIgnoreCase))
    {
      return new FacebookDeleteResultDto(true);
    }

    await socialService.DeletePostAsync(postId, cancellationToken);
    return new FacebookDeleteResultDto(true);
  }

  public async Task<FacebookPostCommentListDto> GetPostCommentsAsync(
    string pageId,
    string postId,
    string? after = null,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    postId = NormalizeRequired(postId, "postId");
    if (UseMockData && IsMockPage(pageId)) return MockComments(postId, after, limit);

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var comments = await socialService.GetCommentsAsync(
      postId,
      account.ZernioAccountId,
      after,
      Math.Clamp(limit, 1, 100),
      cancellationToken);

    return new FacebookPostCommentListDto(
      comments.Items.Select(comment => MapComment(comment, postId)).ToList(),
      null,
      comments.NextCursor,
      null);
  }

  public async Task<FacebookCommentActionResultDto> CommentOnPostAsync(
    string pageId,
    string postId,
    CreateFacebookCommentRequest request,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    postId = NormalizeRequired(postId, "postId");
    var message = NormalizeRequired(request.Message, "message");
    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var response = await SendFormAsync<FacebookPublishResponse>(HttpMethod.Post, $"{postId}/comments", token, new Dictionary<string, string> { ["message"] = message }, cancellationToken);
    return new FacebookCommentActionResultDto(true, response.Id, "Đã bình luận bài viết.");
  }

  public async Task<FacebookCommentActionResultDto> ReplyToCommentAsync(
    string pageId,
    string commentId,
    ReplyFacebookCommentRequest request,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    commentId = NormalizeRequired(commentId, "commentId");
    var postId = NormalizeRequired(request.PostId, "postId");
    var message = NormalizeRequired(request.Message, "message");
    if (UseMockData && IsMockPage(pageId))
      return new FacebookCommentActionResultDto(true, $"mock_reply_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", "MOCK: Đã giả lập trả lời bình luận Facebook.");

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var response = await socialService.ReplyToCommentAsync(
      postId,
      new CreateSocialCommentReplyRequest(account.ZernioAccountId, message, commentId),
      cancellationToken);
    return new FacebookCommentActionResultDto(response.Success, response.Id, response.Message ?? "Đã trả lời bình luận qua Zernio.");
  }

  public async Task<FacebookCommentActionResultDto> ToggleCommentHiddenAsync(
    string pageId,
    string commentId,
    ToggleFacebookCommentHiddenRequest request,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    commentId = NormalizeRequired(commentId, "commentId");
    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var response = await SendFormAsync<FacebookSuccessResponse>(HttpMethod.Post, commentId, token, new Dictionary<string, string> { ["is_hidden"] = request.IsHidden ? "true" : "false" }, cancellationToken);
    return new FacebookCommentActionResultDto(response.Success, commentId, request.IsHidden ? "Đã ẩn bình luận." : "Đã hiện bình luận.");
  }

  public async Task<FacebookDeleteResultDto> DeleteCommentAsync(
    string pageId,
    string commentId,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    commentId = NormalizeRequired(commentId, "commentId");
    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var response = await SendFormAsync<FacebookSuccessResponse>(HttpMethod.Delete, commentId, token, new Dictionary<string, string>(), cancellationToken);
    return new FacebookDeleteResultDto(response.Success);
  }

  public async Task<FacebookConversationListDto> GetConversationsAsync(
    string pageId,
    string? after = null,
    int limit = 25,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    if (UseMockData && IsMockPage(pageId)) return MockConversations(pageId, after, limit);

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var conversations = await socialService.GetConversationsAsync(
      "facebook",
      account.ZernioAccountId,
      account.ZernioProfileId,
      after,
      Math.Clamp(limit, 1, 100),
      cancellationToken);

    return new FacebookConversationListDto(
      conversations.Items.Select(conversation => MapConversation(conversation, account.ZernioAccountId)).ToList(),
      null,
      conversations.NextCursor,
      null);
  }

  public async Task<FacebookMessageListDto> GetConversationMessagesAsync(
    string pageId,
    string conversationId,
    string? before = null,
    int limit = 50,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    conversationId = NormalizeRequired(conversationId, "conversationId");
    if (UseMockData && IsMockPage(pageId)) return MockMessages(pageId, conversationId, before, limit);

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var messages = await socialService.GetConversationMessagesAsync(
      conversationId,
      account.ZernioAccountId,
      before,
      Math.Clamp(limit, 1, 100),
      cancellationToken);

    return new FacebookMessageListDto(
      messages.Items.Select(message => MapMessage(message, conversationId, account.ZernioAccountId)).ToList(),
      null,
      messages.NextCursor,
      null);
  }

  public async Task<FacebookMessageSendResultDto> SendMessageAsync(
    string pageId,
    string conversationId,
    SendFacebookMessageRequest request,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    conversationId = NormalizeRequired(conversationId, "conversationId");
    var hasText = !string.IsNullOrWhiteSpace(request.Text);
    var hasAttachment = !string.IsNullOrWhiteSpace(request.AttachmentUrl);
    if (!hasText && !hasAttachment)
    {
      throw new FacebookApiException("Tin nhắn cần có nội dung hoặc media.", "facebook_message_empty", 400);
    }

    if (UseMockData && IsMockPage(pageId))
    {
      return new FacebookMessageSendResultDto(true, $"mock_message_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
    }

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var response = await socialService.SendMessageAsync(
      conversationId,
      new SendSocialMessageRequest(
        account.ZernioAccountId,
        request.Text,
        request.AttachmentUrl,
        hasAttachment ? NormalizeAttachmentType(request.AttachmentType) : null),
      cancellationToken);

    return new FacebookMessageSendResultDto(response.Success || !string.IsNullOrWhiteSpace(response.Id), response.Id);
  }

  public async Task<MarkConversationReadResultDto> MarkConversationReadAsync(
    string pageId,
    string conversationId,
    CancellationToken cancellationToken = default)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    conversationId = NormalizeRequired(conversationId, "conversationId");
    if (UseMockData && IsMockPage(pageId)) return new MarkConversationReadResultDto(true);

    var account = await ResolveZernioAccountAsync(pageId, cancellationToken);
    var response = await socialService.MarkConversationReadAsync(
      conversationId,
      account.ZernioAccountId,
      cancellationToken);

    return new MarkConversationReadResultDto(response.Success);
  }

  private async Task<string> GetConversationCustomerIdAsync(
    string pageId,
    string conversationId,
    string token,
    CancellationToken cancellationToken)
  {
    var conversation = await SendGetAsync<FacebookConversationResponse>(
      conversationId,
      token,
      new Dictionary<string, string> { ["fields"] = ConversationFields },
      cancellationToken);
    var participant = conversation.Participants?.Data?.FirstOrDefault(item =>
      !string.IsNullOrWhiteSpace(item.Id) && !string.Equals(item.Id, pageId, StringComparison.Ordinal));

    return NormalizeRequired(participant?.Id, "recipientId");
  }

  private static string NormalizeAttachmentType(string? value)
  {
    var type = string.IsNullOrWhiteSpace(value) ? "image" : value.Trim().ToLowerInvariant();
    return type is "image" or "video" or "audio" or "file"
      ? type
      : throw new FacebookApiException("Loại media không hỗ trợ.", "facebook_invalid_attachment_type", 400);
  }

  private async Task<FacebookPublishResultDto> PublishMediaAsync(
    string pageId,
    string edge,
    string fileField,
    Stream stream,
    string fileName,
    string contentType,
    string? text,
    DateTimeOffset? scheduledPublishTime,
    bool published,
    CancellationToken cancellationToken)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    if (stream.Length <= 0)
    {
      throw new FacebookApiException("File tải lên không hợp lệ.", "facebook_invalid_file", 400);
    }

    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var endpoint = BuildUri($"{pageId}/{edge}", token, null);
    using var form = new MultipartFormDataContent();
    var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
    form.Add(fileContent, fileField, fileName);

    if (!string.IsNullOrWhiteSpace(text))
    {
      form.Add(new StringContent(text.Trim()), edge == "videos" ? "description" : "caption");
    }

    form.Add(new StringContent(ShouldSchedule(scheduledPublishTime) || !published ? "false" : "true"), "published");
    if (ShouldSchedule(scheduledPublishTime))
    {
      form.Add(new StringContent(ToUnixSeconds(scheduledPublishTime!.Value).ToString()), "scheduled_publish_time");
    }

    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
    var response = await SendAsync<FacebookPublishResponse>(request, cancellationToken);
    return new FacebookPublishResultDto(response.Id, response.PostId, null);
  }

  private async Task<SocialAccountConnectionDto> ResolveZernioAccountAsync(string pageId, CancellationToken cancellationToken)
  {
    var normalizedPageId = NormalizeRequired(pageId, "pageId");
    var accounts = await socialService.GetAccountsAsync("facebook", sync: true, cancellationToken: cancellationToken);
    var match = accounts.FirstOrDefault(account => account.IsActive && (
      string.Equals(account.ZernioAccountId, normalizedPageId, StringComparison.Ordinal)
      || string.Equals(account.Id.ToString(), normalizedPageId, StringComparison.OrdinalIgnoreCase)));

    if (match is null)
    {
      throw new FacebookApiException("Chưa kết nối trang Facebook qua Zernio hoặc pageId/accountId không hợp lệ.", "zernio_facebook_not_connected", 404);
    }

    return match;
  }

  private static FacebookConnectionDto ToFacebookConnection(SocialAccountConnectionDto account) => new(
    account.ZernioAccountId,
    account.DisplayName ?? account.Username ?? account.ZernioAccountId,
    "ZERNIO",
    null,
    account.LastSyncedAt,
    account.IsActive);

  private static string BuildSocialPostContent(string? message, string? link)
  {
    var content = (message ?? string.Empty).Trim();
    if (!string.IsNullOrWhiteSpace(link))
    {
      content = string.IsNullOrWhiteSpace(content)
        ? link.Trim()
        : content + "\n" + link.Trim();
    }
    return content;
  }

  private static int ParseCursorPage(string? cursor) =>
    int.TryParse(cursor, out var page) && page > 0 ? page : 1;

  private async Task<FacebookConnectionDto> UpsertPageConnectionAsync(
    string pageId,
    string? requestedPageName,
    string fallbackPageName,
    string token,
    DateTimeOffset? expiresAt,
    CancellationToken cancellationToken)
  {
    var connection = await dbContext.FacebookPageConnections
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(x => x.PageId == pageId, cancellationToken);

    var pageName = string.IsNullOrWhiteSpace(requestedPageName) ? fallbackPageName : requestedPageName.Trim();
    if (connection is null)
    {
      connection = new FacebookPageConnection
      {
        PageId = pageId,
        EncryptedPageAccessToken = tokenProtector.Protect(token),
        TokenLast4 = Last4(token),
        PageName = pageName,
        ExpiresAt = expiresAt,
        LastValidatedAt = DateTimeOffset.UtcNow
      };
      dbContext.FacebookPageConnections.Add(connection);
    }
    else
    {
      connection.EncryptedPageAccessToken = tokenProtector.Protect(token);
      connection.TokenLast4 = Last4(token);
      connection.PageName = pageName;
      connection.ExpiresAt = expiresAt;
      connection.LastValidatedAt = DateTimeOffset.UtcNow;
      connection.IsActive = true;
      connection.IsDeleted = false;
      connection.DeletedAt = null;
      connection.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new FacebookConnectionDto(
      connection.PageId,
      connection.PageName,
      connection.TokenLast4,
      connection.ExpiresAt,
      connection.LastValidatedAt,
      connection.IsActive);
  }

  private async Task<string> ExchangeCodeForUserTokenAsync(
    string code,
    string redirectUri,
    CancellationToken cancellationToken)
  {
    var response = await SendGetWithoutAccessTokenAsync<FacebookOAuthAccessTokenResponse>(
      "oauth/access_token",
      new Dictionary<string, string>
      {
        ["client_id"] = NormalizeRequired(facebookApiSettings.AppId, "FacebookApi:AppId"),
        ["client_secret"] = NormalizeRequired(facebookApiSettings.AppSecret, "FacebookApi:AppSecret"),
        ["redirect_uri"] = redirectUri,
        ["code"] = code
      },
      cancellationToken);

    return NormalizeRequired(response.AccessToken, "access_token");
  }

  private async Task<string> ExchangeForLongLivedUserTokenAsync(string shortLivedToken, CancellationToken cancellationToken)
  {
    var response = await SendGetWithoutAccessTokenAsync<FacebookOAuthAccessTokenResponse>(
      "oauth/access_token",
      new Dictionary<string, string>
      {
        ["grant_type"] = "fb_exchange_token",
        ["client_id"] = NormalizeRequired(facebookApiSettings.AppId, "FacebookApi:AppId"),
        ["client_secret"] = NormalizeRequired(facebookApiSettings.AppSecret, "FacebookApi:AppSecret"),
        ["fb_exchange_token"] = shortLivedToken
      },
      cancellationToken);

    return NormalizeRequired(response.AccessToken, "access_token");
  }

  private async Task<(string Token, DateTimeOffset? ExpiresAt)> ValidatePageTokenAsync(
    string pageAccessToken,
    string appAccessToken,
    CancellationToken cancellationToken)
  {
    var debug = await SendGetAsync<FacebookDebugTokenEnvelope>(
      "debug_token",
      appAccessToken,
      new Dictionary<string, string> { ["input_token"] = pageAccessToken },
      cancellationToken);

    if (debug.Data?.IsValid is false)
    {
      throw new FacebookApiException("Page Access Token không hợp lệ.", "facebook_invalid_page_token", 400);
    }

    var expiresAt = debug.Data?.ExpiresAt is > 0
      ? DateTimeOffset.FromUnixTimeSeconds(debug.Data.ExpiresAt.Value)
      : (DateTimeOffset?)null;

    return (pageAccessToken, expiresAt);
  }

  private string ProtectOAuthConnectToken(string pageId, string pageName, string pageAccessToken, DateTimeOffset? tokenExpiresAt)
  {
    var payload = new FacebookOAuthConnectTokenPayload(
      pageId,
      pageName,
      pageAccessToken,
      tokenExpiresAt,
      DateTimeOffset.UtcNow.AddMinutes(15));
    return oauthConnectTokenProtector.Protect(JsonSerializer.Serialize(payload, JsonOptions));
  }

  private FacebookOAuthConnectTokenPayload UnprotectOAuthConnectToken(string connectToken)
  {
    try
    {
      var json = oauthConnectTokenProtector.Unprotect(NormalizeRequired(connectToken, "connectToken"));
      var payload = JsonSerializer.Deserialize<FacebookOAuthConnectTokenPayload>(json, JsonOptions)
        ?? throw new FacebookApiException("Token kết nối Facebook không hợp lệ.", "facebook_oauth_token_invalid", 400);
      if (payload.ExpiresAt <= DateTimeOffset.UtcNow)
      {
        throw new FacebookApiException("Token kết nối Facebook đã hết hạn. Vui lòng lấy lại danh sách fanpage.", "facebook_oauth_token_expired", 400);
      }

      return payload;
    }
    catch (FacebookApiException)
    {
      throw;
    }
    catch (Exception ex)
    {
      throw new FacebookApiException("Token kết nối Facebook không hợp lệ.", "facebook_oauth_token_invalid", 400, innerException: ex);
    }
  }

  private async Task<FacebookPageInfoDto> GetPageInfoWithTokenAsync(string pageId, string token, CancellationToken cancellationToken)
  {
    var response = await SendGetAsync<FacebookPageInfoResponse>(
      pageId,
      token,
      new Dictionary<string, string> { ["fields"] = "id,name,category,link" },
      cancellationToken);

    return new FacebookPageInfoDto(response.Id, response.Name, response.Category, response.Link);
  }

  private async Task<string> GetPageTokenAsync(string pageId, CancellationToken cancellationToken)
  {
    pageId = NormalizeRequired(pageId, "pageId");
    var connection = await dbContext.FacebookPageConnections
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.PageId == pageId && x.IsActive, cancellationToken);

    return UnprotectToken(connection);
  }

  private async Task<string> GetAnyActiveTokenAsync(CancellationToken cancellationToken)
  {
    var connection = await dbContext.FacebookPageConnections
      .AsNoTracking()
      .OrderByDescending(x => x.LastValidatedAt ?? DateTimeOffset.MinValue)
      .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);

    return UnprotectToken(connection);
  }

  private string UnprotectToken(FacebookPageConnection? connection)
  {
    if (connection is null)
    {
      throw new FacebookApiException("Chưa kết nối trang Facebook.", "facebook_not_connected", 404);
    }

    try
    {
      return tokenProtector.Unprotect(connection.EncryptedPageAccessToken);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Không thể giải mã Page Access Token cho PageId={PageId}", connection.PageId);
      throw new FacebookApiException("Không thể đọc token Facebook đã lưu. Vui lòng kết nối lại trang.", "facebook_token_unreadable", 500);
    }
  }

  private async Task<T> SendGetWithoutAccessTokenAsync<T>(
    string path,
    Dictionary<string, string> query,
    CancellationToken cancellationToken)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, null, query));
    return await SendAsync<T>(request, cancellationToken);
  }

  private async Task<T> SendGetAsync<T>(
    string path,
    string accessToken,
    Dictionary<string, string>? query,
    CancellationToken cancellationToken)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, accessToken, query));
    return await SendAsync<T>(request, cancellationToken);
  }

  private async Task<T> SendFormAsync<T>(
    HttpMethod method,
    string path,
    string accessToken,
    Dictionary<string, string> formValues,
    CancellationToken cancellationToken)
  {
    using var request = new HttpRequestMessage(method, BuildUri(path, accessToken, null));
    if (method != HttpMethod.Delete)
    {
      request.Content = new FormUrlEncodedContent(formValues);
    }

    return await SendAsync<T>(request, cancellationToken);
  }

  private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    using var client = httpClientFactory.CreateClient();
    using var response = await client.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      await ThrowFacebookExceptionAsync(response, cancellationToken);
    }

    return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
      ?? throw new FacebookApiException("Facebook trả về dữ liệu rỗng.", "facebook_empty_response", (int)response.StatusCode);
  }

  private Uri BuildUri(string path, string? accessToken, Dictionary<string, string>? query)
  {
    var version = NormalizeVersion(facebookApiSettings.GraphApiVersion);
    var builder = new UriBuilder($"https://graph.facebook.com/{version}/{path.TrimStart('/')}");
    var parameters = new Dictionary<string, string>(query ?? new Dictionary<string, string>());
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
      parameters["access_token"] = accessToken;
    }

    builder.Query = string.Join("&", parameters.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
    return builder.Uri;
  }

  private Uri BuildDialogUri(string path, Dictionary<string, string> query)
  {
    var version = NormalizeVersion(facebookApiSettings.GraphApiVersion);
    var builder = new UriBuilder($"https://www.facebook.com/{version}/dialog/{path.TrimStart('/')}");
    builder.Query = string.Join("&", query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
    return builder.Uri;
  }

  private static async Task ThrowFacebookExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    FacebookErrorEnvelope? envelope = null;
    try
    {
      envelope = JsonSerializer.Deserialize<FacebookErrorEnvelope>(body, JsonOptions);
    }
    catch
    {
      // Keep fallback message.
    }

    var code = envelope?.Error?.Code?.ToString() ?? $"http_{(int)response.StatusCode}";
    var message = envelope?.Error?.Message ?? "Facebook từ chối yêu cầu.";
    var retryAfter = response.Headers.RetryAfter?.Delta;

    throw new FacebookApiException(
      $"Facebook API lỗi: {message}",
      $"facebook_{code}",
      (int)response.StatusCode,
      retryAfter);
  }

  private static FacebookConversationDto MapConversation(SocialConversationDto conversation, string fallbackPageId)
  {
    return new FacebookConversationDto(
      conversation.Id,
      string.IsNullOrWhiteSpace(conversation.AccountId) ? fallbackPageId : conversation.AccountId,
      conversation.ParticipantId,
      conversation.ParticipantName,
      conversation.ParticipantPicture,
      conversation.LastMessage,
      conversation.UpdatedTime,
      conversation.UnreadCount,
      null,
      conversation.Url,
      [
        new FacebookParticipantDto(
          string.IsNullOrWhiteSpace(conversation.AccountId) ? fallbackPageId : conversation.AccountId,
          conversation.AccountUsername,
          null,
          true),
        new FacebookParticipantDto(
          conversation.ParticipantId,
          conversation.ParticipantName,
          null,
          false)
      ]);
  }

  private static FacebookMessageDto MapMessage(SocialMessageDto message, string fallbackConversationId, string pageId)
  {
    var isFromPage = string.Equals(message.Direction, "outgoing", StringComparison.OrdinalIgnoreCase)
      || string.Equals(message.SenderId, pageId, StringComparison.Ordinal);

    return new FacebookMessageDto(
      message.Id,
      string.IsNullOrWhiteSpace(message.ConversationId) ? fallbackConversationId : message.ConversationId,
      message.SenderId,
      message.SenderName,
      isFromPage,
      message.Text,
      message.CreatedAt,
      message.Attachments.Select(MapAttachment).ToList());
  }

  private static FacebookMessageAttachmentDto MapAttachment(SocialMessageAttachmentDto attachment) => new(
    attachment.Type,
    attachment.Url ?? attachment.PreviewUrl,
    attachment.FileName,
    null,
    null);

  private static FacebookPostDto MapPost(SocialPostDto post)
  {
    var platform = post.Platforms.FirstOrDefault(p => string.Equals(p.Platform, "facebook", StringComparison.OrdinalIgnoreCase))
      ?? post.Platforms.FirstOrDefault();
    return new FacebookPostDto(
      string.IsNullOrWhiteSpace(platform?.PlatformPostId) ? post.Id : platform.PlatformPostId!,
      post.Content,
      post.PublishedAt ?? post.ScheduledFor,
      post.PublishedAt,
      platform?.PlatformPostUrl ?? post.PlatformPostUrl,
      post.MediaItems.FirstOrDefault(media => string.Equals(media.Type, "image", StringComparison.OrdinalIgnoreCase))?.Url,
      string.Equals(post.Status, "published", StringComparison.OrdinalIgnoreCase) || post.PublishedAt.HasValue,
      post.ScheduledFor,
      post.Status,
      post.MediaItems.FirstOrDefault()?.Type ?? "status");
  }

  private static FacebookCommentDto MapComment(SocialCommentDto comment, string? fallbackPostId)
  {
    return new FacebookCommentDto(
      comment.Id,
      fallbackPostId,
      comment.ParentId,
      comment.Author is null ? null : new FacebookCommentAuthorDto(comment.Author.Id, comment.Author.Name ?? comment.Author.Username, comment.Author.Picture),
      comment.Message,
      comment.CreatedTime,
      comment.LikeCount,
      comment.ReplyCount,
      comment.CanReply,
      comment.CanHide,
      comment.CanDelete,
      comment.IsHidden,
      comment.Replies.Select(reply => MapComment(reply, fallbackPostId)).ToList());
  }

  private static FacebookPostDto MapPost(FacebookPostResponse post)
  {
    return new FacebookPostDto(
      post.Id,
      post.Message,
      ParseFacebookDate(post.CreatedTime),
      ParseFacebookDate(post.UpdatedTime),
      post.PermalinkUrl,
      post.FullPicture,
      post.IsPublished,
      ParseUnixOrDate(post.ScheduledPublishTime),
      post.StatusType,
      post.Type);
  }

  private static FacebookCommentDto MapComment(FacebookCommentResponse comment, string? fallbackPostId)
  {
    var parentId = comment.Parent?.Id;
    return new FacebookCommentDto(
      comment.Id,
      fallbackPostId,
      parentId,
      MapAuthor(comment.From),
      comment.Message,
      ParseFacebookDate(comment.CreatedTime),
      comment.LikeCount,
      comment.CommentCount,
      comment.CanComment,
      comment.CanRemove,
      comment.CanRemove,
      comment.IsHidden,
      (comment.Comments?.Data ?? Array.Empty<FacebookCommentResponse>()).Select(reply => MapComment(reply, fallbackPostId)).ToList());
  }

  private static FacebookCommentAuthorDto? MapAuthor(FacebookActorResponse? actor)
  {
    return actor is null
      ? null
      : new FacebookCommentAuthorDto(actor.Id, actor.Name, actor.Picture?.Data?.Url);
  }

  private static FacebookConversationDto MapConversation(FacebookConversationResponse conversation, string pageId)
  {
    var participants = (conversation.Participants?.Data ?? Array.Empty<FacebookParticipantResponse>())
      .Select(participant => new FacebookParticipantDto(
        participant.Id,
        participant.Name,
        participant.Email,
        string.Equals(participant.Id, pageId, StringComparison.Ordinal)))
      .ToList();
    var customer = participants.FirstOrDefault(participant => !participant.IsPage);

    return new FacebookConversationDto(
      conversation.Id,
      pageId,
      customer?.Id,
      customer?.Name,
      null,
      conversation.Snippet,
      ParseFacebookDate(conversation.UpdatedTime),
      conversation.UnreadCount,
      conversation.MessageCount,
      conversation.Link,
      participants);
  }

  private static FacebookMessageDto MapMessage(FacebookMessageResponse message, string conversationId, string pageId)
  {
    return new FacebookMessageDto(
      message.Id,
      conversationId,
      message.From?.Id,
      message.From?.Name,
      string.Equals(message.From?.Id, pageId, StringComparison.Ordinal),
      message.Message,
      ParseFacebookDate(message.CreatedTime),
      MapAttachments(message.Attachments));
  }

  private static IReadOnlyList<FacebookMessageAttachmentDto> MapAttachments(FacebookAttachmentListResponse? attachments)
  {
    return (attachments?.Data ?? Array.Empty<FacebookAttachmentResponse>())
      .Select(attachment => new FacebookMessageAttachmentDto(
        attachment.MimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ? "image" : attachment.Type,
        attachment.ImageData?.Url ?? attachment.VideoData?.Url ?? attachment.FileUrl,
        attachment.Name,
        attachment.MimeType,
        attachment.Size))
      .ToList();
  }

  private static DateTimeOffset? ParseFacebookDate(string? value)
  {
    return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
  }

  private static DateTimeOffset? ParseUnixOrDate(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    if (long.TryParse(value, out var unix)) return DateTimeOffset.FromUnixTimeSeconds(unix);
    return ParseFacebookDate(value);
  }

  private static bool ShouldSchedule(DateTimeOffset? scheduledPublishTime)
  {
    return scheduledPublishTime.HasValue;
  }

  private static long ToUnixSeconds(DateTimeOffset value)
  {
    return value.ToUniversalTime().ToUnixTimeSeconds();
  }

  private static string NormalizeRequired(string? value, string field)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new FacebookApiException($"Thiếu thông tin {field}.", "facebook_validation_error", 400);
    }

    return value.Trim();
  }

  private static string NormalizeAbsoluteUri(string? value, string field)
  {
    var uri = NormalizeRequired(value, field);
    if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
    {
      throw new FacebookApiException($"{field} không hợp lệ.", "facebook_validation_error", 400);
    }

    return parsed.ToString();
  }

  private static string NormalizeVersion(string version)
  {
    if (string.IsNullOrWhiteSpace(version)) return "v25.0";
    version = version.Trim();
    return version.StartsWith('v') ? version : $"v{version}";
  }

  private static string Last4(string value)
  {
    return value.Length <= 4 ? value : value[^4..];
  }

  private sealed record FacebookOAuthConnectTokenPayload(
    string PageId,
    string PageName,
    string PageAccessToken,
    DateTimeOffset? TokenExpiresAt,
    DateTimeOffset ExpiresAt);

  private sealed record FacebookOAuthAccessTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn);

  private sealed record FacebookOAuthAccountResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("tasks")] IReadOnlyList<string>? Tasks);

  private sealed record FacebookDebugTokenEnvelope([property: JsonPropertyName("data")] FacebookDebugTokenData? Data);

  private sealed record FacebookDebugTokenData(
    [property: JsonPropertyName("is_valid")] bool IsValid,
    [property: JsonPropertyName("expires_at")] long? ExpiresAt);

  private sealed record FacebookPublishResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("post_id")] string? PostId);

  private sealed record FacebookSuccessResponse([property: JsonPropertyName("success")] bool Success);

  private sealed record FacebookPageInfoResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("link")] string? Link);

  private sealed record FacebookListResponse<T>(
    [property: JsonPropertyName("data")] IReadOnlyList<T> Data,
    [property: JsonPropertyName("paging")] FacebookPaging? Paging);

  private sealed record FacebookPaging(
    [property: JsonPropertyName("cursors")] FacebookCursors? Cursors,
    [property: JsonPropertyName("next")] string? Next);

  private sealed record FacebookCursors(
    [property: JsonPropertyName("before")] string? Before,
    [property: JsonPropertyName("after")] string? After);

  private sealed record FacebookPostResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("created_time")] string? CreatedTime,
    [property: JsonPropertyName("updated_time")] string? UpdatedTime,
    [property: JsonPropertyName("permalink_url")] string? PermalinkUrl,
    [property: JsonPropertyName("full_picture")] string? FullPicture,
    [property: JsonPropertyName("is_published")] bool? IsPublished,
    [property: JsonPropertyName("scheduled_publish_time")] string? ScheduledPublishTime,
    [property: JsonPropertyName("status_type")] string? StatusType,
    [property: JsonPropertyName("type")] string? Type);

  private sealed record FacebookCommentResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parent")] FacebookIdResponse? Parent,
    [property: JsonPropertyName("from")] FacebookActorResponse? From,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("created_time")] string? CreatedTime,
    [property: JsonPropertyName("like_count")] int? LikeCount,
    [property: JsonPropertyName("comment_count")] int? CommentCount,
    [property: JsonPropertyName("can_comment")] bool? CanComment,
    [property: JsonPropertyName("can_remove")] bool? CanRemove,
    [property: JsonPropertyName("is_hidden")] bool? IsHidden,
    [property: JsonPropertyName("comments")] FacebookListResponse<FacebookCommentResponse>? Comments);

  private sealed record FacebookIdResponse([property: JsonPropertyName("id")] string? Id);

  private sealed record FacebookActorResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("picture")] FacebookPictureResponse? Picture);

  private sealed record FacebookPictureResponse([property: JsonPropertyName("data")] FacebookPictureDataResponse? Data);

  private sealed record FacebookPictureDataResponse([property: JsonPropertyName("url")] string? Url);

  private sealed record FacebookConversationResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("updated_time")] string? UpdatedTime,
    [property: JsonPropertyName("message_count")] int? MessageCount,
    [property: JsonPropertyName("unread_count")] int? UnreadCount,
    [property: JsonPropertyName("snippet")] string? Snippet,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("participants")] FacebookListResponse<FacebookParticipantResponse>? Participants);

  private sealed record FacebookParticipantResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("email")] string? Email);

  private sealed record FacebookMessageResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_time")] string? CreatedTime,
    [property: JsonPropertyName("from")] FacebookActorResponse? From,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("attachments")] FacebookAttachmentListResponse? Attachments);

  private sealed record FacebookAttachmentListResponse([property: JsonPropertyName("data")] IReadOnlyList<FacebookAttachmentResponse> Data);

  private sealed record FacebookAttachmentResponse(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("mime_type")] string? MimeType,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("file_url")] string? FileUrl,
    [property: JsonPropertyName("image_data")] FacebookAttachmentMediaResponse? ImageData,
    [property: JsonPropertyName("video_data")] FacebookAttachmentMediaResponse? VideoData);

  private sealed record FacebookAttachmentMediaResponse([property: JsonPropertyName("url")] string? Url);

  private sealed record FacebookMessengerSendResponse(
    [property: JsonPropertyName("recipient_id")] string? RecipientId,
    [property: JsonPropertyName("message_id")] string? MessageId);

  private sealed record FacebookErrorEnvelope([property: JsonPropertyName("error")] FacebookError? Error);

  private sealed record FacebookError(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("code")] int? Code,
    [property: JsonPropertyName("error_subcode")] int? ErrorSubcode,
    [property: JsonPropertyName("fbtrace_id")] string? TraceId);
}
