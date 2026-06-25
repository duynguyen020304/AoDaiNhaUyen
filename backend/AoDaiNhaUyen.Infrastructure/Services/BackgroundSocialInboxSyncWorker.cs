using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class BackgroundSocialInboxSyncWorker(
  IServiceScopeFactory scopeFactory,
  IOptions<SocialInboxSyncOptions> options,
  IOptions<SocialAutoReplyOptions> autoReplyOptions,
  ILogger<BackgroundSocialInboxSyncWorker> logger) : BackgroundService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly SocialInboxSyncOptions _options = options.Value;
  private readonly SocialAutoReplyOptions _autoReplyOptions = autoReplyOptions.Value;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        if (_options.Enabled)
        {
          await SyncAsync(stoppingToken);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Social inbox sync worker lỗi.");
      }

      await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, _options.PollIntervalSeconds)), stoppingToken);
    }
  }

  private async Task SyncAsync(CancellationToken cancellationToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var socialService = scope.ServiceProvider.GetRequiredService<ISocialService>();
    var hermesEvents = scope.ServiceProvider.GetRequiredService<IHermesEventOutboxPublisher>();

    var batchSize = Math.Clamp(_options.AccountBatchSize, 1, 50);
    var accounts = await dbContext.SocialAccountConnections
      .AsNoTracking()
      .Where(x => x.Platform == "facebook" && x.IsActive)
      .OrderBy(x => x.LastSyncedAt ?? DateTimeOffset.MinValue)
      .ThenBy(x => x.CreatedAt)
      .Take(batchSize)
      .ToListAsync(cancellationToken);

    foreach (var account in accounts)
    {
      try
      {
        if (_options.SyncComments)
        {
          await SyncCommentsAsync(dbContext, socialService, hermesEvents, account.ZernioAccountId, account.ZernioProfileId, cancellationToken);
        }

        if (_options.SyncMessages)
        {
          await SyncMessagesAsync(dbContext, socialService, account.ZernioAccountId, account.ZernioProfileId, account.AutoReplyIgnoreBefore, cancellationToken);
        }

        await dbContext.SocialAccountConnections
          .Where(x => x.Id == account.Id)
          .ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.LastSyncedAt, DateTimeOffset.UtcNow)
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Không sync được social inbox cho account {AccountId}.", account.ZernioAccountId);
      }
    }
  }

  private async Task SyncCommentsAsync(
    AppDbContext dbContext,
    ISocialService socialService,
    IHermesEventOutboxPublisher hermesEvents,
    string accountId,
    string profileId,
    CancellationToken cancellationToken)
  {
    var socialAutomationInitializedAt = await GetSocialAutomationInitializedAtAsync(dbContext, cancellationToken);
    var posts = await socialService.GetCommentedPostsAsync(
      "facebook",
      accountId,
      profileId,
      cursor: null,
      limit: Math.Clamp(_options.ListLimit, 1, 100),
      cancellationToken);

    foreach (var post in posts.Items.Take(Math.Clamp(_options.MaxPostsPerAccount, 1, 100)))
    {
      var existingCommentIds = await GetExistingCommentIdsAsync(dbContext, accountId, post.Id, cancellationToken);
      var comments = await socialService.GetCommentsAsync(
        post.Id,
        accountId,
        cursor: null,
        limit: Math.Clamp(_options.ItemLimit, 1, 100),
        cancellationToken);

      foreach (var comment in Flatten(comments.Items).Where(x => x.Author?.IsOwner != true))
      {
        if (string.IsNullOrWhiteSpace(comment.Id) || existingCommentIds.Contains(comment.Id)) continue;
        if (IsBeforeSocialAutomationCutoff(comment.CreatedTime, socialAutomationInitializedAt)) continue;

        await EnqueueSocialEventAsync(
          hermesEvents,
          "social_comment_received",
          "comment.created",
          comment.Platform ?? "facebook",
          comment.Id,
          accountId,
          post.Id,
          isComment: true,
          cancellationToken);
        existingCommentIds.Add(comment.Id);
      }
    }
  }

  private async Task SyncMessagesAsync(
    AppDbContext dbContext,
    ISocialService socialService,
    string accountId,
    string profileId,
    DateTimeOffset? autoReplyIgnoreBefore,
    CancellationToken cancellationToken)
  {
    var socialAutomationInitializedAt = await GetSocialAutomationInitializedAtAsync(dbContext, cancellationToken);
    if (socialAutomationInitializedAt is null)
    {
      logger.LogWarning("Không tạo auto-reply message vì thiếu social automation cutoff global.");
    }

    var conversations = await socialService.GetConversationsAsync(
      "facebook",
      accountId,
      profileId,
      cursor: null,
      limit: Math.Clamp(_options.ListLimit, 1, 100),
      cancellationToken);

    foreach (var conversation in conversations.Items.Take(Math.Clamp(_options.MaxConversationsPerAccount, 1, 100)))
    {
      var messages = await socialService.GetConversationMessagesAsync(
        conversation.Id,
        accountId,
        cursor: null,
        limit: Math.Clamp(_options.ItemLimit, 1, 100),
        cancellationToken);

      if (socialAutomationInitializedAt is null) continue;

      var eligibleMessages = messages.Items
        .Select(message => NormalizePolledMessage(message, accountId, conversation.Id))
        .Where(IsIncomingCustomerMessage)
        .Where(message => ShouldProcessPolledMessage(message, socialAutomationInitializedAt.Value, autoReplyIgnoreBefore))
        .OrderBy(message => message.CreatedAt)
        .ToList();
      if (eligibleMessages.Count == 0) continue;

      var existingReceiptIds = await GetExistingReceiptMessageIdsAsync(dbContext, eligibleMessages, cancellationToken);
      foreach (var message in eligibleMessages)
      {
        if (existingReceiptIds.Contains(message.Id)) continue;

        var batch = await EnqueueOrExtendAutoReplyBatchAsync(dbContext, message, cancellationToken);
        await UpsertSyntheticWebhookReceiptAsync(dbContext, message, batch.Id, cancellationToken);
        existingReceiptIds.Add(message.Id);
      }
    }
  }

  private static async Task<HashSet<string>> GetExistingCommentIdsAsync(AppDbContext dbContext, string accountId, string postId, CancellationToken cancellationToken)
  {
    var normalizedAccountId = accountId.Trim();
    var normalizedPostId = postId.Trim();
    if (string.IsNullOrWhiteSpace(normalizedAccountId) || string.IsNullOrWhiteSpace(normalizedPostId))
    {
      return new HashSet<string>(StringComparer.Ordinal);
    }

    var existing = await dbContext.SocialInboxComments
      .IgnoreQueryFilters()
      .AsNoTracking()
      .Where(x => x.AccountId == normalizedAccountId && x.PostId == normalizedPostId && !x.IsDeleted)
      .Select(x => x.CommentId)
      .ToListAsync(cancellationToken);

    return existing.ToHashSet(StringComparer.Ordinal);
  }

  private static SocialMessageDto NormalizePolledMessage(SocialMessageDto message, string accountId, string conversationId)
  {
    return message with
    {
      AccountId = string.IsNullOrWhiteSpace(message.AccountId) ? accountId : message.AccountId,
      ConversationId = string.IsNullOrWhiteSpace(message.ConversationId) ? conversationId : message.ConversationId,
      Platform = string.IsNullOrWhiteSpace(message.Platform) ? "facebook" : message.Platform
    };
  }

  private bool ShouldProcessPolledMessage(SocialMessageDto message, DateTimeOffset socialAutomationInitializedAt, DateTimeOffset? autoReplyIgnoreBefore)
  {
    if (string.IsNullOrWhiteSpace(message.Id) || string.IsNullOrWhiteSpace(message.AccountId) || string.IsNullOrWhiteSpace(message.ConversationId)) return false;
    if (message.CreatedAt is null) return false;
    if (message.CreatedAt < socialAutomationInitializedAt) return false;
    return !_autoReplyOptions.SkipBacklogBeforeConnection || autoReplyIgnoreBefore is null || message.CreatedAt >= autoReplyIgnoreBefore;
  }

  private static async Task<HashSet<string>> GetExistingReceiptMessageIdsAsync(AppDbContext dbContext, IReadOnlyCollection<SocialMessageDto> messages, CancellationToken cancellationToken)
  {
    var ids = messages
      .Select(x => x.Id)
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Distinct(StringComparer.Ordinal)
      .ToList();
    if (ids.Count == 0) return new HashSet<string>(StringComparer.Ordinal);

    var accountIds = messages
      .Select(x => x.AccountId?.Trim())
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Distinct(StringComparer.Ordinal)
      .ToList();
    var platforms = messages
      .Select(x => NormalizePlatform(x.Platform ?? "facebook"))
      .Distinct(StringComparer.Ordinal)
      .ToList();

    var existing = await dbContext.SocialWebhookReceipts
      .IgnoreQueryFilters()
      .AsNoTracking()
      .Where(x => ids.Contains(x.MessageId) && accountIds.Contains(x.AccountId) && platforms.Contains(x.Platform))
      .Select(x => x.MessageId)
      .ToListAsync(cancellationToken);

    return existing.ToHashSet(StringComparer.Ordinal);
  }

  private async Task<SocialAutoReplyBatch> EnqueueOrExtendAutoReplyBatchAsync(AppDbContext dbContext, SocialMessageDto message, CancellationToken cancellationToken)
  {
    var platform = NormalizePlatform(message.Platform ?? "facebook");
    var accountId = NormalizeRequired(message.AccountId, "accountId");
    var conversationId = NormalizeRequired(message.ConversationId, "conversationId");
    var now = DateTimeOffset.UtcNow;
    var windowEnds = now.AddSeconds(Math.Clamp(_autoReplyOptions.DebounceSeconds, 1, 300));

    var batch = await dbContext.SocialAutoReplyBatches
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(x => x.Platform == platform && x.AccountId == accountId && x.ConversationId == conversationId && x.Status == "pending", cancellationToken);

    if (batch is null)
    {
      batch = new SocialAutoReplyBatch
      {
        Id = Guid.NewGuid(),
        Platform = platform,
        AccountId = accountId,
        ConversationId = conversationId,
        Status = "pending",
        WindowStartedAt = now,
        WindowEndsAt = windowEnds,
        LastMessageAt = message.CreatedAt ?? now,
        MessageIdsJson = JsonSerializer.Serialize(new[] { message.Id }, JsonOptions),
        MessageCount = 1,
        CreatedAt = now.UtcDateTime,
        UpdatedAt = now.UtcDateTime
      };
      dbContext.SocialAutoReplyBatches.Add(batch);
    }
    else if (batch.Status == "pending")
    {
      var ids = DeserializeMessageIds(batch.MessageIdsJson);
      if (!ids.Contains(message.Id, StringComparer.Ordinal)) ids.Add(message.Id);
      batch.MessageIdsJson = JsonSerializer.Serialize(ids, JsonOptions);
      batch.MessageCount = ids.Count;
      batch.LastMessageAt = message.CreatedAt ?? now;
      batch.WindowEndsAt = windowEnds;
      batch.UpdatedAt = now.UtcDateTime;
    }

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      foreach (var entry in dbContext.ChangeTracker.Entries<SocialAutoReplyBatch>().Where(x => x.State == EntityState.Added))
      {
        entry.State = EntityState.Detached;
      }

      batch = await dbContext.SocialAutoReplyBatches
        .IgnoreQueryFilters()
        .FirstAsync(x => x.Platform == platform && x.AccountId == accountId && x.ConversationId == conversationId && x.Status == "pending", cancellationToken);
      if (batch.Status == "pending")
      {
        var ids = DeserializeMessageIds(batch.MessageIdsJson);
        if (!ids.Contains(message.Id, StringComparer.Ordinal)) ids.Add(message.Id);
        batch.MessageIdsJson = JsonSerializer.Serialize(ids, JsonOptions);
        batch.MessageCount = ids.Count;
        batch.LastMessageAt = message.CreatedAt ?? now;
        batch.WindowEndsAt = windowEnds;
        batch.UpdatedAt = now.UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
      }
    }

    return batch;
  }

  private static async Task UpsertSyntheticWebhookReceiptAsync(AppDbContext dbContext, SocialMessageDto message, Guid batchId, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(message.Id) || string.IsNullOrWhiteSpace(message.AccountId) || string.IsNullOrWhiteSpace(message.ConversationId)) return;

    var platform = NormalizePlatform(message.Platform ?? "facebook");
    var accountId = message.AccountId.Trim();
    var existing = await dbContext.SocialWebhookReceipts
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(x => x.Platform == platform && x.AccountId == accountId && x.MessageId == message.Id, cancellationToken);

    if (existing is null)
    {
      existing = new SocialWebhookReceipt
      {
        Provider = "zernio-polling",
        Platform = platform,
        EventType = "message.polled",
        AccountId = accountId,
        ThreadId = message.ConversationId.Trim(),
        MessageId = message.Id,
        Direction = NormalizeDirection(message.Direction),
        OccurredAt = message.CreatedAt,
        ReceivedAt = DateTimeOffset.UtcNow,
        ReplyStatus = "batched",
        SkipReason = $"batch:{batchId:N}",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.SocialWebhookReceipts.Add(existing);
    }
    else
    {
      existing.ReplyStatus = "batched";
      existing.SkipReason = $"batch:{batchId:N}";
      existing.ThreadId = message.ConversationId.Trim();
      existing.Direction = NormalizeDirection(message.Direction);
      existing.OccurredAt = message.CreatedAt ?? existing.OccurredAt;
      existing.IsActive = true;
      existing.IsDeleted = false;
      existing.DeletedAt = null;
      existing.UpdatedAt = DateTime.UtcNow;
    }

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      foreach (var entry in dbContext.ChangeTracker.Entries<SocialWebhookReceipt>().Where(x => x.State == EntityState.Added))
      {
        entry.State = EntityState.Detached;
      }
    }
  }

  private static async Task<DateTimeOffset?> GetSocialAutomationInitializedAtAsync(AppDbContext dbContext, CancellationToken cancellationToken)
  {
    return await dbContext.SocialAutomationStates
      .AsNoTracking()
      .Where(x => x.Key == "global")
      .Select(x => (DateTimeOffset?)x.InitializedAt)
      .FirstOrDefaultAsync(cancellationToken);
  }

  private static bool IsBeforeSocialAutomationCutoff(DateTimeOffset? occurredAt, DateTimeOffset? initializedAt) =>
    occurredAt is not null && initializedAt is not null && occurredAt < initializedAt;

  private static Task EnqueueSocialEventAsync(
    IHermesEventOutboxPublisher hermesEvents,
    string eventType,
    string eventName,
    string platform,
    string aggregateId,
    string pageId,
    string threadId,
    bool isComment,
    CancellationToken cancellationToken)
  {
    var safePlatform = string.IsNullOrWhiteSpace(platform) ? "facebook" : platform.Trim().ToLowerInvariant();
    return hermesEvents.EnqueueAdminEventAsync(
      eventType,
      "SocialInbox",
      aggregateId,
      new
      {
        EventName = eventName,
        Platform = safePlatform,
        AggregateId = aggregateId,
        PageId = pageId,
        ThreadId = threadId,
        CommentId = isComment ? aggregateId : null,
        ConversationId = isComment ? null : threadId,
        ContainsUserGeneratedText = true,
        Source = "polling",
        Privacy = "message/comment body and author PII omitted; fetch body via admin API to reply"
      },
      $"{eventType}:{safePlatform}:{eventName}:{aggregateId}",
      $"social:{safePlatform}:{aggregateId}",
      cancellationToken);
  }

  private static bool IsIncomingCustomerMessage(SocialMessageDto message) =>
    string.Equals(NormalizeDirection(message.Direction), "incoming", StringComparison.Ordinal);

  private static string NormalizeDirection(string? direction)
  {
    var value = string.IsNullOrWhiteSpace(direction) ? "incoming" : direction.Trim().ToLowerInvariant();
    return value is "outgoing" or "sent" ? "outgoing" : "incoming";
  }

  private static string NormalizePlatform(string? platform) =>
    string.IsNullOrWhiteSpace(platform) ? "facebook" : platform.Trim().ToLowerInvariant();

  private static string NormalizeRequired(string? value, string name)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
    return value.Trim();
  }

  private static List<string> DeserializeMessageIds(string? json)
  {
    if (string.IsNullOrWhiteSpace(json)) return [];
    try
    {
      return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList() ?? [];
    }
    catch (JsonException)
    {
      return [];
    }
  }

  private static IEnumerable<SocialCommentDto> Flatten(IEnumerable<SocialCommentDto> comments)
  {
    foreach (var comment in comments)
    {
      yield return comment;
      foreach (var reply in Flatten(comment.Replies)) yield return reply;
    }
  }
}
