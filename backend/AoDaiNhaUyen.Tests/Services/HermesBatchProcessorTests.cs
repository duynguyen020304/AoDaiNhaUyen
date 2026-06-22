using System.Net;
using System.Text.Json;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

/// <summary>
/// Covers the batch path: N events -> one Hermes call -> ONE comprehensive report,
/// plus the failure/fallback signalling the worker relies on.
/// </summary>
public sealed class HermesBatchProcessorTests
{
  private const string AssistantText = "Báo cáo tổng hợp cho cửa hàng.";

  [Fact]
  public async Task ProcessBatchAsync_HappyPath_CreatesSingleReport_AndCompletesAllEvents()
  {
    var db = CreateDb();
    var events = SeedEvents(db, ("checkout_completed", "Order"), ("low_stock", "Inventory"), ("blog_seo_opportunity", "Content"));
    var handler = new StubHandler(_ => Ok(AssistantText));
    var processor = CreateProcessor(db, handler);

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(events.Select(e => e.Id).OrderBy(x => x), processed.OrderBy(x => x));
    Assert.Equal(1, handler.CallCount);

    var run = Assert.Single(db.HermesRuns.ToList());
    Assert.Equal("admin_event_batch", run.Trigger);
    Assert.Equal("completed", run.Status);

    var report = Assert.Single(db.HermesReports.ToList());
    Assert.Equal(run.ConversationId, report.CorrelationId); // CorrelationId == batchId
    Assert.Equal(run.Id, report.RunId);
    Assert.Contains("3 sự kiện", report.Title);
    Assert.Contains(AssistantText, report.Summary);

    Assert.All(db.HermesEventOutbox.ToList(), e => Assert.Equal("completed", e.Status));
    // One batch_member + one agent_response trace per event.
    Assert.Equal(events.Count, db.HermesAgentTraceSteps.Count(t => t.Kind == "batch_member"));
    Assert.Equal(events.Count, db.HermesAgentTraceSteps.Count(t => t.Kind == "agent_response"));
  }

  [Fact]
  public async Task ProcessBatchAsync_AggregatesSeverityToMax()
  {
    var db = CreateDb();
    // low_stock -> warning (risk profile); checkout_completed -> info (revenue profile).
    var events = SeedEvents(db, ("checkout_completed", "Order"), ("low_stock", "Inventory"));
    var processor = CreateProcessor(db, new StubHandler(_ => Ok(AssistantText)));

    await processor.ProcessBatchAsync(events, CancellationToken.None);

    var report = Assert.Single(db.HermesReports.ToList());
    Assert.Equal("warning", report.Severity);
  }

  [Fact]
  public async Task ProcessBatchAsync_HttpFailure_ReturnsEmpty_NoReport_NoStatusChange()
  {
    var db = CreateDb();
    var events = SeedEvents(db, ("checkout_completed", "Order"), ("low_stock", "Inventory"));
    var processor = CreateProcessor(db, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") }));

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Empty(processed);
    Assert.Empty(db.HermesReports.ToList());
    Assert.Equal("failed", Assert.Single(db.HermesRuns.ToList()).Status);
    Assert.All(db.HermesEventOutbox.ToList(), e => Assert.Equal("processing", e.Status)); // untouched -> worker falls back
  }

  [Fact]
  public async Task ProcessBatchAsync_UnparseableButSuccessfulBody_StillProducesReport()
  {
    // A 2xx response always yields a report (ExtractAssistantText backfills a default
    // when it can't parse content). Only an HTTP-level failure triggers worker fallback.
    var db = CreateDb();
    var events = SeedEvents(db, ("checkout_completed", "Order"), ("low_stock", "Inventory"));
    var processor = CreateProcessor(db, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") }));

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(2, processed.Count);
    Assert.Single(db.HermesReports.ToList());
  }

  [Fact]
  public async Task ProcessBatchAsync_InvalidPayload_ExcludedFromBatch()
  {
    var db = CreateDb();
    var events = SeedEvents(db, ("checkout_completed", "Order"), ("low_stock", "Inventory"), ("promo_created", "Promotion"));
    events[1].PayloadJson = "{not-json"; // poison one
    db.SaveChanges();
    var processor = CreateProcessor(db, new StubHandler(_ => Ok(AssistantText)));

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.DoesNotContain(events[1].Id, processed);
    Assert.Equal(2, processed.Count);
    var report = Assert.Single(db.HermesReports.ToList());
    Assert.Contains("2 sự kiện", report.Title);
  }

  [Fact]
  public async Task ProcessBatchAsync_SingleValidEvent_DelegatesToPerEvent()
  {
    var db = CreateDb();
    var events = SeedEvents(db, ("checkout_completed", "Order"));
    var processor = CreateProcessor(db, new StubHandler(_ => Ok(AssistantText)));

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(events[0].Id, Assert.Single(processed));
    var run = Assert.Single(db.HermesRuns.ToList());
    Assert.Equal("admin_event", run.Trigger); // per-event path, not batch
    var report = Assert.Single(db.HermesReports.ToList());
    Assert.Equal(events[0].Id.ToString("N"), report.CorrelationId); // per-event correlation
  }

  // ---- helpers ----

  private static HttpResponseMessage Ok(string assistantText)
  {
    var body = JsonSerializer.Serialize(new
    {
      output = new[]
      {
        new { type = "message", content = new[] { new { text = assistantText } } }
      }
    });
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
  }

  private static List<HermesEventOutbox> SeedEvents(AppDbContext db, params (string EventType, string AggregateType)[] specs)
  {
    var now = DateTimeOffset.UtcNow;
    var list = specs.Select((s, i) => new HermesEventOutbox
    {
      Id = Guid.NewGuid(),
      EventType = s.EventType,
      AggregateType = s.AggregateType,
      AggregateId = Guid.NewGuid().ToString("N"),
      PayloadJson = "{\"k\":\"v\"}",
      Status = "processing",
      OccurredAt = now.AddSeconds(i),
      ScheduledAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    }).ToList();
    db.HermesEventOutbox.AddRange(list);
    db.SaveChanges();
    return list;
  }

  private static HermesEventProcessor CreateProcessor(AppDbContext db, HttpMessageHandler handler)
  {
    var agentOptions = Options.Create(new HermesAgentOptions { ApiServerUrl = "https://hermes.test", ApiServerKey = "key" });
    var outboxOptions = Options.Create(new HermesOutboxOptions { DryRun = false, BatchProcessingEnabled = true });
    return new HermesEventProcessor(new StubHttpClientFactory(handler), agentOptions, outboxOptions, db, NullLogger<HermesEventProcessor>.Instance);
  }

  private static AppDbContext CreateDb()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"hermes-batch-{Guid.NewGuid():N}")
      .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
      .Options;
    return new AppDbContext(options);
  }

  private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
  {
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      CallCount++;
      return Task.FromResult(responder(request));
    }
  }

  private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
  }
}
