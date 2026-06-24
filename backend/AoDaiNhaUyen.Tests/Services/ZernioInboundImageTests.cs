using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Facebook;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

/// <summary>
/// Offline tests for the inbound-image persistence hook inside
/// <see cref="ZernioService.UpsertMessagesAsync"/> (via
/// <c>TryPersistInboundImageAsync</c>). Verifies success criteria #2 (image
/// bytes stored to private S3 with a stable key) and #9 (failures are
/// non-fatal — StoredImageKey stays null and the message is still saved, so
/// the agent can ask the customer to resend instead of the conversation
/// breaking).
/// </summary>
public sealed class ZernioInboundImageTests
{
  private const string WebhookJsonTemplate = """
    {
      "event": "message.received",
      "account": { "id": "__ACCOUNT_ID__", "platform": "facebook" },
      "conversation": { "id": "__CONVERSATION_ID__", "status": "active" },
      "message": {
        "id": "__MESSAGE_ID__",
        "direction": "incoming",
        "text": "Cho minh thu mau nay nhe",
        "attachments": [ { "type": "image", "url": "__IMAGE_URL__", "filename": "photo.jpg" } ]
      }
    }
    """;

  [Fact]
  public async Task IngestMessageWithImage_WhenDownloadSucceeds_PersistsStoredImageKey()
  {
    await using var db = CreateInMemoryDbContext();
    var imageBytes = new byte[] { 1, 2, 3, 4 };
    var facebook = new StubFacebookService("page-123", "https://cdn.fb/photo.jpg", imageBytes, "image/jpeg");
    var storage = new CapturingStorageService();
    var service = CreateZernioService(db, facebook, storage);

    await service.IngestZernioWebhookAsync(BuildPayload("msg-1", imageUrl: "https://cdn.fb/photo.jpg").RootElement, CancellationToken.None);

    var message = await db.SocialInboxMessages.AsNoTracking().SingleAsync();
    Assert.Equal("msg-1", message.MessageId);
    Assert.NotNull(message.StoredImageKey);
    Assert.Equal("image/jpeg", message.StoredImageMimeType);
    Assert.Contains("private/social-inbox/conv-1", storage.LastUpload?.Folder ?? string.Empty);
    Assert.Equal(imageBytes, facebook.DownloadCalls.Single().Bytes);
  }

  [Fact]
  public async Task IngestMessageWithImage_WhenFacebookReturnsNull_LeavesStoredImageKeyNullWithoutThrowing()
  {
    await using var db = CreateInMemoryDbContext();
    // StubFacebookService returns null for the download (token missing / URL expired).
    var facebook = new StubFacebookService(downloadResult: null);
    var storage = new CapturingStorageService();
    var service = CreateZernioService(db, facebook, storage);

    await service.IngestZernioWebhookAsync(BuildPayload("msg-2", imageUrl: "https://cdn.fb/expired.jpg").RootElement, CancellationToken.None);

    var message = await db.SocialInboxMessages.AsNoTracking().SingleAsync();
    // Critical: message is still persisted (conversation does not break), but no stored image.
    Assert.Equal("msg-2", message.MessageId);
    Assert.Null(message.StoredImageKey);
    Assert.Null(storage.LastUpload); // upload never attempted
  }

  [Fact]
  public async Task IngestMessageWithImage_WhenStorageThrows_LeavesStoredImageKeyNullWithoutThrowing()
  {
    await using var db = CreateInMemoryDbContext();
    var imageBytes = new byte[] { 9, 8, 7 };
    var facebook = new StubFacebookService("page-123", "https://cdn.fb/photo.jpg", imageBytes, "image/png");
    var storage = new ThrowingStorageService();
    var service = CreateZernioService(db, facebook, storage);

    // Must NOT throw — the persist hook swallows storage failures.
    await service.IngestZernioWebhookAsync(BuildPayload("msg-3", imageUrl: "https://cdn.fb/photo.jpg").RootElement, CancellationToken.None);

    var message = await db.SocialInboxMessages.AsNoTracking().SingleAsync();
    Assert.Equal("msg-3", message.MessageId);
    Assert.Null(message.StoredImageKey); // upload failed -> stays null
    Assert.NotNull(message.AttachmentsJson); // original metadata still recorded
  }

  private static JsonDocument BuildPayload(string messageId, string accountId = "page-123", string conversationId = "conv-1", string imageUrl = "https://cdn.fb/photo.jpg")
  {
    var json = WebhookJsonTemplate
      .Replace("__ACCOUNT_ID__", accountId)
      .Replace("__CONVERSATION_ID__", conversationId)
      .Replace("__MESSAGE_ID__", messageId)
      .Replace("__IMAGE_URL__", imageUrl);
    return JsonDocument.Parse(json);
  }

  private static AppDbContext CreateInMemoryDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new AppDbContext(options);
  }

  private static ZernioService CreateZernioService(AppDbContext db, IFacebookService facebook, IStorageService storage)
  {
    var scopeFactory = new StubServiceScopeFactory(facebook);
    var zernioOptions = Options.Create(new ZernioSettings
    {
      ApiUrl = "https://zernio.example.com",
      ApiKey = "test",
      WebhookSecret = "test"
    });
    var syncOptions = Options.Create(new SocialInboxSyncOptions { DownloadInboundImages = true });
    return new ZernioService(
      httpClientFactory: new StubHttpClientFactory(),
      zernioOptions: zernioOptions,
      dbContext: db,
      logger: NullLogger<ZernioService>.Instance,
      storageService: storage,
      socialInboxSyncOptions: syncOptions,
      serviceScopeFactory: scopeFactory,
      hermesEventOutboxPublisher: null);
  }

  private sealed class StubHttpClientFactory : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => new();
  }

  /// <summary>
  /// Resolves a fixed <see cref="IFacebookService"/> from a transient scope, mirroring
  /// how <c>ZernioService.TryPersistInboundImageAsync</c> resolves the service lazily
  /// to avoid the constructor cycle (FacebookService -> ISocialService -> ZernioService).
  /// </summary>
  private sealed class StubServiceScopeFactory(IFacebookService facebook) : IServiceScopeFactory
  {
    public IServiceScope CreateScope() => new StubScope(facebook);
  }

  private sealed class StubScope(IFacebookService facebook) : IServiceScope
  {
    public IServiceProvider ServiceProvider { get; } = new StubProvider(facebook);
    public void Dispose() { }
  }

  private sealed class StubProvider(IFacebookService facebook) : IServiceProvider
  {
    public object? GetService(Type serviceType) => serviceType == typeof(IFacebookService) ? facebook : null;
  }

  private sealed class StubFacebookService : IFacebookService
  {
    private readonly Dictionary<(string PageId, string Url), (byte[] Bytes, string Mime)> _downloads = new();
    private readonly FacebookAttachmentDownloadDto? _downloadResult;

    public List<FacebookAttachmentDownloadDto> DownloadCalls { get; } = new();

    public StubFacebookService() { }
    public StubFacebookService(string pageId, string url, byte[] bytes, string mime)
      => _downloads[(pageId, url)] = (bytes, mime);
    public StubFacebookService(FacebookAttachmentDownloadDto? downloadResult) => _downloadResult = downloadResult;

    public Task<FacebookAttachmentDownloadDto?> DownloadAttachmentBytesAsync(string pageId, string attachmentUrl, long maxBytes, CancellationToken ct = default)
    {
      if (_downloadResult is not null)
      {
        DownloadCalls.Add(_downloadResult);
        return Task.FromResult<FacebookAttachmentDownloadDto?>(_downloadResult);
      }
      if (_downloads.TryGetValue((pageId, attachmentUrl), out var hit))
      {
        var dto = new FacebookAttachmentDownloadDto(hit.Bytes, hit.Mime);
        DownloadCalls.Add(dto);
        return Task.FromResult<FacebookAttachmentDownloadDto?>(dto);
      }
      return Task.FromResult<FacebookAttachmentDownloadDto?>(null);
    }

    // The rest of the interface is unused by these tests.
    public Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookConnectionDto> ConnectPageAsync(ConnectFacebookPageRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookOAuthUrlDto> GetOAuthUrlAsync(string redirectUri, string state, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<FacebookOAuthPageDto>> GetOAuthPagesAsync(FacebookOAuthPagesRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookConnectionDto> ConnectOAuthPageAsync(ConnectFacebookOAuthPageRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DisconnectPageAsync(string pageId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPageInfoDto> GetPageInfoAsync(string pageId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishPostAsync(string pageId, CreateFacebookPostRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishPhotoAsync(string pageId, Stream s, string f, string ct2, string? c, DateTimeOffset? t = null, bool p = true, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishVideoAsync(string pageId, Stream s, string f, string ct2, string? d, DateTimeOffset? t = null, bool p = true, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostListDto> GetPostsAsync(string pageId, string? c = null, int l = 25, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostDto> GetPostAsync(string postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostDto> UpdatePostAsync(string postId, UpdateFacebookPostRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookDeleteResultDto> DeletePostAsync(string postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostCommentListDto> GetPostCommentsAsync(string p1, string p2, string? a = null, int l = 25, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookCommentActionResultDto> CommentOnPostAsync(string p1, string p2, CreateFacebookCommentRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookCommentActionResultDto> ReplyToCommentAsync(string p1, string c, ReplyFacebookCommentRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookCommentActionResultDto> ToggleCommentHiddenAsync(string p1, string c, ToggleFacebookCommentHiddenRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookDeleteResultDto> DeleteCommentAsync(string p1, string c, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookConversationListDto> GetConversationsAsync(string p, string? a = null, int l = 25, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookMessageListDto> GetConversationMessagesAsync(string p1, string p2, string? b = null, int l = 50, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookMessageSendResultDto> SendMessageAsync(string p1, string p2, SendFacebookMessageRequest r, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<MarkConversationReadResultDto> MarkConversationReadAsync(string p1, string p2, CancellationToken ct = default) => throw new NotImplementedException();
  }

  private sealed class CapturingStorageService : IStorageService
  {
    public (string FileName, string ContentType, string? Folder)? LastUpload { get; private set; }
    public string? LastObjectKey { get; private set; }

    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default)
    {
      LastUpload = (fileName, contentType, folder);
      LastObjectKey = $"{folder}/{fileName}";
      return Task.FromResult(new UploadedFileResult(LastObjectKey, LastObjectKey, LastObjectKey, contentType, 0, fileName));
    }
    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default) => Task.FromResult($"https://storage.example.com/{objectKey}?sig=test");
    public Task DeleteAsync(string objectKey, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) => throw new NotImplementedException();
    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) => Task.FromResult(true);
    public string BuildCanonicalUrl(string objectKey) => $"https://storage.example.com/{objectKey}";
    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default) => Task.FromResult($"https://storage.example.com/public/{objectKey}");
    public bool IsConfigured() => true;
  }

  private sealed class ThrowingStorageService : IStorageService
  {
    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default)
      => throw new InvalidOperationException("S3 down");
    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default) => Task.FromResult("https://x");
    public Task DeleteAsync(string objectKey, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) => throw new NotImplementedException();
    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) => Task.FromResult(true);
    public string BuildCanonicalUrl(string objectKey) => "https://x";
    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default) => Task.FromResult("https://x");
    public bool IsConfigured() => true;
  }
}
