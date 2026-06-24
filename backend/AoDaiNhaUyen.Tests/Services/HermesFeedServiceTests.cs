using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class HermesFeedServiceTests
{
  [Fact]
  public async Task GetRecentFeedAsync_AttachesFanOutRunAndReportViaTraceLinkedRunId()
  {
    await using var db = CreateDb();
    var eventId = Guid.NewGuid();
    var runId = Guid.NewGuid();
    var fanOutId = Guid.NewGuid().ToString("N");
    var now = DateTimeOffset.UtcNow;

    db.HermesEventOutbox.Add(new HermesEventOutbox
    {
      Id = eventId,
      EventType = "checkout_completed",
      AggregateType = "Order",
      AggregateId = Guid.NewGuid().ToString("N"),
      PayloadJson = "{\"orderId\":\"A123\"}",
      Status = "completed",
      OccurredAt = now,
      ScheduledAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });

    db.HermesRuns.Add(new HermesRun
    {
      Id = runId,
      Status = "completed",
      Trigger = "admin_event_fanout",
      ConversationId = fanOutId,
      PromptPreview = "preview",
      ResultPreview = "preview",
      StartedAt = now,
      CompletedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });

    db.HermesAgentTraceSteps.Add(new HermesAgentTraceStep
    {
      Id = Guid.NewGuid(),
      EventOutboxId = eventId,
      RunId = runId,
      Kind = "partial_report",
      Title = "partial",
      Summary = "partial summary",
      Status = "success",
      StartedAt = now,
      CompletedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });

    db.HermesReports.Add(new HermesReport
    {
      Id = Guid.NewGuid(),
      ReportType = "mixed",
      Severity = "warning",
      Title = "Fan-out final report",
      Summary = "compressed summary",
      PayloadJson = "{}",
      Source = "hermes_agent",
      CorrelationId = fanOutId,
      RunId = runId,
      Status = "open",
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });

    await db.SaveChangesAsync();

    var service = new HermesFeedService(db);
    var snapshot = await service.GetRecentFeedAsync(10, CancellationToken.None);

    var item = Assert.Single(snapshot.Items);
    Assert.Equal("completed", item.RunStatus);
    Assert.Contains(item.HermesMessages, x => x.Kind == "report" && x.Title == "Fan-out final report");
  }

  private static AppDbContext CreateDb()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"hermes-feed-{Guid.NewGuid():N}")
      .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
      .Options;
    return new AppDbContext(options);
  }
}
