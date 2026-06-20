using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Facebook;
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
  ILogger<FacebookService> logger) : IFacebookService
{
  private const string PostFields = "id,message,created_time,updated_time,permalink_url,full_picture,is_published,scheduled_publish_time,status_type,type";
  private const string OAuthScopes = "pages_show_list,pages_read_engagement,pages_manage_posts,pages_manage_metadata";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly FacebookApiSettings facebookApiSettings = facebookApiSettings.Value;
  private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("AoDaiNhaUyen.Facebook.PageAccessToken.v1");
  private readonly IDataProtector oauthConnectTokenProtector = dataProtectionProvider.CreateProtector("AoDaiNhaUyen.Facebook.OAuthConnectToken.v1");

  public async Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken = default)
  {
    return await dbContext.FacebookPageConnections
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
    if (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrWhiteSpace(request.Link))
    {
      throw new FacebookApiException("Bài viết cần có nội dung hoặc liên kết.", "facebook_post_empty", 400);
    }

    var parameters = new Dictionary<string, string>
    {
      ["message"] = request.Message?.Trim() ?? string.Empty,
      ["published"] = ShouldSchedule(request.ScheduledPublishTime) || !request.Published ? "false" : "true"
    };

    if (!string.IsNullOrWhiteSpace(request.Link))
    {
      parameters["link"] = request.Link.Trim();
    }

    if (ShouldSchedule(request.ScheduledPublishTime))
    {
      parameters["scheduled_publish_time"] = ToUnixSeconds(request.ScheduledPublishTime!.Value).ToString();
    }

    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var response = await SendFormAsync<FacebookPublishResponse>(HttpMethod.Post, $"{pageId}/feed", token, parameters, cancellationToken);
    return new FacebookPublishResultDto(response.Id, null, null);
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
    var token = await GetPageTokenAsync(pageId, cancellationToken);
    var query = new Dictionary<string, string>
    {
      ["fields"] = PostFields,
      ["limit"] = Math.Clamp(limit, 1, 100).ToString()
    };

    if (!string.IsNullOrWhiteSpace(cursor))
    {
      query["after"] = cursor.Trim();
    }

    var response = await SendGetAsync<FacebookListResponse<FacebookPostResponse>>($"{pageId}/feed", token, query, cancellationToken);
    return new FacebookPostListDto(
      response.Data.Select(MapPost).ToList(),
      response.Paging?.Cursors?.Before,
      response.Paging?.Cursors?.After,
      response.Paging?.Next);
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
    var token = await GetAnyActiveTokenAsync(cancellationToken);
    var response = await SendFormAsync<FacebookSuccessResponse>(HttpMethod.Delete, postId, token, new Dictionary<string, string>(), cancellationToken);
    return new FacebookDeleteResultDto(response.Success);
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

  private sealed record FacebookErrorEnvelope([property: JsonPropertyName("error")] FacebookError? Error);

  private sealed record FacebookError(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("code")] int? Code,
    [property: JsonPropertyName("error_subcode")] int? ErrorSubcode,
    [property: JsonPropertyName("fbtrace_id")] string? TraceId);
}
