using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
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
  ILogger<BackgroundSocialInboxSyncWorker> logger) : BackgroundService
{
  private readonly SocialInboxSyncOptions _options = options.Value;

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

      await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds)), stoppingToken);
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
          await SyncMessagesAsync(socialService, hermesEvents, account.ZernioAccountId, account.ZernioProfileId, cancellationToken);
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
      var comments = await socialService.GetCommentsAsync(
        post.Id,
        accountId,
        cursor: null,
        limit: Math.Clamp(_options.ItemLimit, 1, 100),
        cancellationToken);

      foreach (var comment in Flatten(comments.Items).Where(x => x.Author?.IsOwner != true))
      {
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
      }
    }
  }

  private async Task SyncMessagesAsync(
    ISocialService socialService,
    IHermesEventOutboxPublisher hermesEvents,
    string accountId,
    string profileId,
    CancellationToken cancellationToken)
  {
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

      // Message auto-replies are driven by signed webhooks + durable debounce batches.
      // Polling only hydrates inbox state; enqueueing here would replay old backlog after reconnect/DB restore.
      _ = messages;
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

  private static IEnumerable<SocialCommentDto> Flatten(IEnumerable<SocialCommentDto> comments)
  {
    foreach (var comment in comments)
    {
      yield return comment;
      foreach (var reply in Flatten(comment.Replies)) yield return reply;
    }
  }
}
