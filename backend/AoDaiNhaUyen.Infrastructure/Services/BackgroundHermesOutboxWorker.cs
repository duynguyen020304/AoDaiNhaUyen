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

public sealed class BackgroundHermesOutboxWorker(
  IServiceScopeFactory scopeFactory,
  IOptions<HermesOutboxOptions> options,
  ILogger<BackgroundHermesOutboxWorker> logger) : BackgroundService
{
  private readonly HermesOutboxOptions _options = options.Value;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        if (_options.Enabled)
        {
          await ProcessBatchAsync(stoppingToken);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Hermes outbox worker lỗi.");
      }

      await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
    }
  }

  private async Task ProcessBatchAsync(CancellationToken cancellationToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var processor = scope.ServiceProvider.GetRequiredService<IHermesEventProcessor>();

    await RequeueStaleProcessingAsync(dbContext, cancellationToken);

    var runner = string.IsNullOrWhiteSpace(_options.RunnerName) ? "aodai-hermes-outbox-worker" : _options.RunnerName;
    var batchSize = Math.Clamp(_options.BatchSize, 1, 100);

    var claimedIds = await dbContext.Database
      .SqlQueryRaw<Guid>(
        """
        UPDATE hermes_event_outbox
        SET status = 'processing', locked_by = {0}, locked_at = NOW(), updated_at = NOW()
        WHERE id IN (
          SELECT id
          FROM hermes_event_outbox
          WHERE status IN ('pending','failed')
            AND scheduled_at <= NOW()
            AND attempts < max_attempts
          ORDER BY scheduled_at, occurred_at
          FOR UPDATE SKIP LOCKED
          LIMIT {1}
        )
        RETURNING id AS "Value"
        """,
        runner,
        batchSize)
      .ToListAsync(cancellationToken);

    if (claimedIds.Count > 0)
    {
      var now = DateTimeOffset.UtcNow;
      dbContext.HermesAgentTraceSteps.AddRange(claimedIds.Select(id => new Domain.Entities.HermesAgentTraceStep
      {
        Id = Guid.NewGuid(),
        EventOutboxId = id,
        Kind = "claimed",
        Title = "Worker đã claim event",
        Summary = $"Runner {runner} đã claim event từ outbox để xử lý.",
        Status = "success",
        StartedAt = now,
        CompletedAt = now,
        CreatedAt = now.UtcDateTime,
        UpdatedAt = now.UtcDateTime
      }));
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    if (_options.BatchProcessingEnabled && claimedIds.Count > 1)
    {
      await ProcessClaimedAsBatchesAsync(dbContext, processor, claimedIds, cancellationToken);
    }
    else
    {
      foreach (var id in claimedIds)
      {
        var item = await dbContext.HermesEventOutbox.FirstAsync(x => x.Id == id, cancellationToken);
        await ProcessItemAsync(dbContext, processor, item, cancellationToken);
      }
    }
  }

  private async Task ProcessClaimedAsBatchesAsync(
    AppDbContext dbContext,
    IHermesEventProcessor processor,
    IReadOnlyList<Guid> claimedIds,
    CancellationToken cancellationToken)
  {
    // Preserve claim order (scheduled_at, occurred_at) so batches group naturally.
    var items = await dbContext.HermesEventOutbox
      .Where(x => claimedIds.Contains(x.Id))
      .ToListAsync(cancellationToken);
    var ordered = claimedIds
      .Select(id => items.First(x => x.Id == id))
      .ToList();

    foreach (var chunk in ChunkItems(ordered))
    {
      IReadOnlyList<Guid> processedIds;
      try
      {
        processedIds = await processor.ProcessBatchAsync(chunk, cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Hermes batch ({Count} events) lỗi, fallback xử lý từng event.", chunk.Count);
        processedIds = Array.Empty<Guid>();
      }

      var processed = processedIds.ToHashSet();

      // The processor already committed status='completed' for events it returned
      // (inside its own transaction). Every other event in the chunk made no durable
      // change — retry it per-event so its own retry/backoff/dead-letter applies.
      foreach (var item in chunk.Where(x => !processed.Contains(x.Id)))
      {
        await ProcessItemAsync(dbContext, processor, item, cancellationToken);
      }
    }
  }

  private List<List<HermesEventOutbox>> ChunkItems(IReadOnlyList<HermesEventOutbox> items)
  {
    var maxEvents = Math.Clamp(_options.MaxBatchEvents, 1, 100);
    var maxBytes = _options.MaxBatchPayloadBytes;
    var chunks = new List<List<HermesEventOutbox>>();
    var current = new List<HermesEventOutbox>();
    var currentBytes = 0;

    foreach (var item in items)
    {
      var itemBytes = System.Text.Encoding.UTF8.GetByteCount(item.PayloadJson);
      if (current.Count > 0 && (current.Count >= maxEvents || (maxBytes > 0 && currentBytes + itemBytes > maxBytes)))
      {
        chunks.Add(current);
        current = new List<HermesEventOutbox>();
        currentBytes = 0;
      }
      current.Add(item);
      currentBytes += itemBytes;
    }
    if (current.Count > 0) chunks.Add(current);
    return chunks;
  }

  private async Task RequeueStaleProcessingAsync(AppDbContext dbContext, CancellationToken cancellationToken)
  {
    var staleBefore = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(Math.Max(1, _options.LockTimeoutMinutes)));
    var retryAt = DateTimeOffset.UtcNow.AddMinutes(1);

    await dbContext.HermesEventOutbox
      .Where(x => x.Status == "processing" && x.LockedAt < staleBefore && x.Attempts < x.MaxAttempts - 1)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.Status, "failed")
        .SetProperty(x => x.Attempts, x => x.Attempts + 1)
        .SetProperty(x => x.ScheduledAt, retryAt)
        .SetProperty(x => x.LockedAt, (DateTimeOffset?)null)
        .SetProperty(x => x.LockedBy, (string?)null)
        .SetProperty(x => x.LastError, "Hermes worker recovered stale processing event.")
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken);

    await dbContext.HermesEventOutbox
      .Where(x => x.Status == "processing" && x.LockedAt < staleBefore && x.Attempts >= x.MaxAttempts - 1)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.Status, "dead")
        .SetProperty(x => x.Attempts, x => x.Attempts + 1)
        .SetProperty(x => x.LockedAt, (DateTimeOffset?)null)
        .SetProperty(x => x.LockedBy, (string?)null)
        .SetProperty(x => x.LastError, "Hermes worker marked stale event dead.")
        .SetProperty(x => x.ProcessedAt, DateTimeOffset.UtcNow)
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken);
  }

  private static async Task ProcessItemAsync(
    AppDbContext dbContext,
    IHermesEventProcessor processor,
    Domain.Entities.HermesEventOutbox item,
    CancellationToken cancellationToken)
  {
    try
    {
      await processor.ProcessAsync(item, cancellationToken);
      item.Status = "completed";
      item.ProcessedAt = DateTimeOffset.UtcNow;
      item.LockedAt = null;
      item.LockedBy = null;
      item.LastError = null;
      item.UpdatedAt = DateTime.UtcNow;
    }
    catch (Exception ex)
    {
      item.Attempts += 1;
      item.Status = item.Attempts >= item.MaxAttempts ? "dead" : "failed";
      item.LastError = NormalizeOptionalText(ex.Message);
      item.ScheduledAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(Math.Pow(2, item.Attempts), 60));
      item.ProcessedAt = item.Status == "dead" ? DateTimeOffset.UtcNow : null;
      item.LockedAt = null;
      item.LockedBy = null;
      item.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static string? NormalizeOptionalText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return value.Trim();
  }
}
