using System.Text.Json;
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

public sealed class BackgroundSocialAutoReplyBatchWorker(
  IServiceScopeFactory scopeFactory,
  IOptions<SocialAutoReplyOptions> options,
  ILogger<BackgroundSocialAutoReplyBatchWorker> logger) : BackgroundService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly SocialAutoReplyOptions _options = options.Value;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        if (_options.Enabled)
        {
          await ProcessDueBatchesAsync(stoppingToken);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Social auto-reply batch worker lỗi.");
      }

      await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, _options.PollIntervalSeconds)), stoppingToken);
    }
  }

  private async Task ProcessDueBatchesAsync(CancellationToken cancellationToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hermesEvents = scope.ServiceProvider.GetRequiredService<IHermesEventOutboxPublisher>();
    var runner = "aodai-social-auto-reply-worker";
    var batchSize = Math.Clamp(_options.BatchSize, 1, 100);

    var claimedIds = await dbContext.Database
      .SqlQueryRaw<Guid>(
        """
        UPDATE social_auto_reply_batches
        SET status = 'processing', locked_by = {0}, locked_at = NOW(), updated_at = NOW()
        WHERE id IN (
          SELECT id
          FROM social_auto_reply_batches
          WHERE status = 'pending'
            AND window_ends_at <= NOW()
            AND NOT is_deleted
          ORDER BY window_ends_at
          FOR UPDATE SKIP LOCKED
          LIMIT {1}
        )
        RETURNING id AS "Value"
        """,
        runner,
        batchSize)
      .ToListAsync(cancellationToken);

    foreach (var id in claimedIds)
    {
      await ProcessBatchAsync(dbContext, hermesEvents, id, cancellationToken);
    }
  }

  private async Task ProcessBatchAsync(
    AppDbContext dbContext,
    IHermesEventOutboxPublisher hermesEvents,
    Guid batchId,
    CancellationToken cancellationToken)
  {
    var batch = await dbContext.SocialAutoReplyBatches.FirstAsync(x => x.Id == batchId, cancellationToken);
    try
    {
      var messageIds = DeserializeMessageIds(batch.MessageIdsJson);

      var payload = new
      {
        EventName = "message.batch.received",
        Platform = batch.Platform,
        PageId = batch.AccountId,
        ConversationId = batch.ConversationId,
        BatchId = batch.Id,
        BatchMessageIds = messageIds,
        BatchMessageCount = batch.MessageCount,
        WindowStartedAt = batch.WindowStartedAt,
        WindowEndsAt = batch.WindowEndsAt,
        ContainsUserGeneratedText = true,
        Source = "debounced_batch",
        ReplyInstruction = "Khách gửi nhiều tin nhắn liên tiếp. Hãy dùng list_facebook_conversation_messages để đọc ngữ cảnh, rồi nếu cần trả lời thì dùng send_facebook_message đúng 1 lần với một tin nhắn tiếng Việt ngắn gọn, gom ý, không tách nhiều câu trả lời.",
        Privacy = "message/comment body and author PII omitted; fetch body via admin API to reply"
      };

      await hermesEvents.EnqueueAdminEventAsync(
        "social_message_batch_received",
        "SocialInboxBatch",
        batch.Id.ToString("N"),
        payload,
        $"social_message_batch_received:{batch.Platform}:{batch.AccountId}:{batch.ConversationId}:{batch.Id:N}",
        $"social:{batch.Platform}:{batch.ConversationId}",
        cancellationToken);

      batch.Status = "queued";
      batch.ProcessedAt = DateTimeOffset.UtcNow;
      batch.LockedAt = null;
      batch.LockedBy = null;
      batch.LastError = null;
      batch.UpdatedAt = DateTime.UtcNow;
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      batch.Status = "failed";
      batch.LastError = NormalizeOptionalText(ex.Message);
      batch.LockedAt = null;
      batch.LockedBy = null;
      batch.ProcessedAt = DateTimeOffset.UtcNow;
      batch.UpdatedAt = DateTime.UtcNow;
      await dbContext.SaveChangesAsync(cancellationToken);
      logger.LogWarning(ex, "Không xử lý được social auto-reply batch {BatchId}.", batchId);
    }
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

  private static string? NormalizeOptionalText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return value.Trim();
  }
}
