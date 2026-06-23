using System.Net;
using System.Text.Json;
using AoDaiNhaUyen.Application.Interfaces.Services;
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

  [Fact]
  public async Task ProcessBatchAsync_FanOutFanIn_CreatesOneFinalReport()
  {
    var db = CreateDb();
    var events = SeedEvents(db,
      ("checkout_completed", "Order"), ("low_stock", "Inventory"), ("blog_seo_opportunity", "Content"),
      ("promo_created", "Promotion"), ("email_campaign", "Campaign"), ("social_anomaly", "Social"),
      ("review_negative", "Review"), ("high_value_order", "Order"), ("config_changed", "Config"));
    var handler = new RecordingHandler(_ => Ok("Báo cáo thành phần."));
    var compressor = new StubCompressor(new HermesCompressedReportResult(
      "Báo cáo cuối",
      "Tổng hợp cuối",
      "warning",
      "mixed",
      ["Tín hiệu chính"],
      ["Theo dõi xử lý"],
      "## Nhận định\nTổng hợp cuối\n\n## Hành động đã thực hiện\nĐã phân tích.\n\n## Kết quả & Tác động\nMột báo cáo duy nhất.\n\n## Mức ưu tiên\nwarning"));
    var processor = CreateProcessor(db, handler, FanOutOptions(), compressor);

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(events.Select(e => e.Id).OrderBy(x => x), processed.OrderBy(x => x));
    Assert.Equal(3, handler.CallCount);
    Assert.Equal(1, compressor.CallCount);
    Assert.All(handler.EventCounts, count => Assert.Equal(3, count));

    var run = Assert.Single(db.HermesRuns.ToList());
    Assert.Equal("admin_event_fanout", run.Trigger);
    Assert.Equal("completed", run.Status);

    var report = Assert.Single(db.HermesReports.ToList());
    Assert.Equal("Báo cáo cuối", report.Title);
    Assert.Equal("mixed", report.ReportType);
    Assert.Equal("warning", report.Severity);

    Assert.Equal(3, db.HermesAgentTraceSteps.Count(t => t.Kind == "partial_report"));
    Assert.Equal(1, db.HermesAgentTraceSteps.Count(t => t.Kind == "fan_in_compression"));
    Assert.All(db.HermesEventOutbox.ToList(), e => Assert.Equal("completed", e.Status));
  }

  [Fact]
  public async Task ProcessBatchAsync_FanOutFanIn_RemainderChunkProcessedImmediately()
  {
    var db = CreateDb();
    var events = SeedEvents(db, Enumerable.Range(0, 10).Select(i => ($"checkout_completed_{i}", "Order")).ToArray());
    var handler = new RecordingHandler(_ => Ok("Báo cáo thành phần."));
    var processor = CreateProcessor(db, handler, FanOutOptions(), new StubCompressor());

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(10, processed.Count);
    Assert.Equal(new[] { 3, 3, 3, 1 }, handler.EventCounts);
    Assert.Equal(4, db.HermesAgentTraceSteps.Count(t => t.Kind == "partial_report"));
    Assert.Single(db.HermesReports.ToList());
    Assert.All(db.HermesEventOutbox.ToList(), e => Assert.Equal("completed", e.Status));
  }

  [Fact]
  public async Task ProcessBatchAsync_FanOutFanIn_FiresSubBatchesConcurrently()
  {
    var db = CreateDb();
    var events = SeedEvents(db, Enumerable.Range(0, 9).Select(i => ($"checkout_completed_{i}", "Order")).ToArray());
    var handler = new RecordingHandler(_ => Ok("Báo cáo thành phần."), TimeSpan.FromMilliseconds(100));
    var processor = CreateProcessor(db, handler, FanOutOptions(), new StubCompressor());

    await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.True(handler.MaxConcurrentCalls > 1);
    Assert.True(handler.MaxConcurrentCalls <= 3);
  }

  [Fact]
  public async Task ProcessBatchAsync_FanOutFanIn_SubBatchFailure_CompletesOnlySuccessfulEvents()
  {
    var db = CreateDb();
    var events = SeedEvents(db, Enumerable.Range(0, 6).Select(i => ($"checkout_completed_{i}", "Order")).ToArray());
    var handler = new RecordingHandler(request =>
    {
      var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
      return body.Contains("\"subBatchIndex\":1")
        ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") }
        : Ok("Báo cáo thành phần.");
    });
    var processor = CreateProcessor(db, handler, FanOutOptions(), new StubCompressor());

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(3, processed.Count);
    Assert.Equal(3, db.HermesEventOutbox.Count(e => e.Status == "completed"));
    Assert.Equal(3, db.HermesEventOutbox.Count(e => e.Status == "processing"));
    Assert.Single(db.HermesReports.ToList());
    Assert.Equal("partial", Assert.Single(db.HermesRuns.ToList()).Status);
  }

  [Fact]
  public async Task ProcessBatchAsync_FanInCompressorThrows_UsesProcessorFallbackAndCompletes()
  {
    var db = CreateDb();
    var events = SeedEvents(db, Enumerable.Range(0, 6).Select(i => ($"checkout_completed_{i}", "Order")).ToArray());
    var handler = new RecordingHandler(_ => Ok("Báo cáo thành phần."));
    var processor = CreateProcessor(db, handler, FanOutOptions(), new ThrowingCompressor());

    var processed = await processor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(6, processed.Count);
    Assert.Single(db.HermesReports.ToList());
    var run = Assert.Single(db.HermesRuns.ToList());
    Assert.Equal("completed", run.Status);
    Assert.Contains("AI compression failed", run.Error);
    Assert.Equal(1, db.HermesAgentTraceSteps.Count(t => t.Kind == "fan_in_compression_failed"));
  }

  [Fact]
  public async Task ProcessBatchAsync_ReusesPersistedPartialReports_AfterCompressorCrash()
  {
    var db = CreateDb();
    var events = SeedEvents(db, Enumerable.Range(0, 6).Select(i => ($"checkout_completed_{i}", "Order")).ToArray());
    var firstHandler = new RecordingHandler(_ => Ok("Báo cáo thành phần."));
    var failingProcessor = CreateProcessor(db, firstHandler, FanOutOptions(), new ThrowingCompressor());

    var firstProcessed = await failingProcessor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(6, firstProcessed.Count);
    Assert.Equal(2, firstHandler.CallCount);
    Assert.Equal(2, db.HermesAgentTraceSteps.Count(t => t.Kind == "partial_report"));
    Assert.Equal(2, db.HermesFanOutSubBatches.Count());

    foreach (var item in events)
    {
      item.Status = "processing";
      item.ProcessedAt = null;
      item.UpdatedAt = DateTime.UtcNow;
    }
    db.HermesReports.RemoveRange(db.HermesReports);
    await db.SaveChangesAsync();

    var secondHandler = new RecordingHandler(_ => Ok("KHÔNG NÊN GỌI LẠI"));
    var successProcessor = CreateProcessor(db, secondHandler, FanOutOptions(), new StubCompressor());

    var secondProcessed = await successProcessor.ProcessBatchAsync(events, CancellationToken.None);

    Assert.Equal(6, secondProcessed.Count);
    Assert.Equal(0, secondHandler.CallCount);
    Assert.Single(db.HermesReports.ToList());
    Assert.Equal(1, db.HermesAgentTraceSteps.Count(t => t.Kind == "fan_out_resumed"));
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

  private static HermesOutboxOptions FanOutOptions() => new()
  {
    DryRun = false,
    BatchProcessingEnabled = true,
    FanOutFanInEnabled = true,
    FanOutSubBatchSize = 3,
    MaxParallelFanOutBatches = 3,
    FanInCompressionEnabled = true,
    FanInFallbackToConcatenation = true
  };

  private static HermesEventProcessor CreateProcessor(
    AppDbContext db,
    HttpMessageHandler handler,
    HermesOutboxOptions? outbox = null,
    IHermesReportCompressorService? compressor = null)
  {
    var agentOptions = Options.Create(new HermesAgentOptions { ApiServerUrl = "https://hermes.test", ApiServerKey = "key" });
    var outboxOptions = Options.Create(outbox ?? new HermesOutboxOptions { DryRun = false, BatchProcessingEnabled = true });
    return new HermesEventProcessor(
      new StubHttpClientFactory(handler),
      agentOptions,
      outboxOptions,
      compressor ?? new StubCompressor(),
      db,
      NullLogger<HermesEventProcessor>.Instance);
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

  private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder, TimeSpan? delay = null) : HttpMessageHandler
  {
    private int _activeCalls;
    private int _callCount;
    private int _maxConcurrentCalls;
    private readonly List<int> _eventCounts = [];
    private readonly object _gate = new();

    public int CallCount => _callCount;
    public int MaxConcurrentCalls => _maxConcurrentCalls;
    public IReadOnlyList<int> EventCounts
    {
      get { lock (_gate) return _eventCounts.ToArray(); }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var active = Interlocked.Increment(ref _activeCalls);
      Interlocked.Increment(ref _callCount);
      UpdateMax(active);
      try
      {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var eventCount = ExtractEventCount(body);
        lock (_gate) _eventCounts.Add(eventCount);
        if (delay is { } wait) await Task.Delay(wait, cancellationToken);
        return responder(request);
      }
      finally
      {
        Interlocked.Decrement(ref _activeCalls);
      }
    }

    private void UpdateMax(int active)
    {
      while (true)
      {
        var current = _maxConcurrentCalls;
        if (active <= current) return;
        if (Interlocked.CompareExchange(ref _maxConcurrentCalls, active, current) == current) return;
      }
    }

    private static int ExtractEventCount(string requestJson)
    {
      using var doc = JsonDocument.Parse(requestJson);
      if (!doc.RootElement.TryGetProperty("metadata", out var metadata)) return 0;
      return metadata.TryGetProperty("eventCount", out var count) ? count.GetInt32() : 0;
    }
  }

  private sealed class StubCompressor(HermesCompressedReportResult? result = null) : IHermesReportCompressorService
  {
    public int CallCount { get; private set; }

    public Task<HermesCompressedReportResult> CompressAsync(IReadOnlyList<HermesPartialReportInput> partialReports, CancellationToken cancellationToken)
    {
      CallCount++;
      return Task.FromResult(result ?? new HermesCompressedReportResult(
        "Báo cáo tổng hợp Hermes",
        "Tổng hợp các báo cáo thành phần.",
        partialReports.Any(x => x.Severity == "warning") ? "warning" : "info",
        partialReports.Select(x => x.ReportType).Distinct().Count() > 1 ? "mixed" : partialReports.FirstOrDefault()?.ReportType ?? "growth",
        [],
        [],
        "## Nhận định\nTổng hợp các báo cáo thành phần.\n\n## Hành động đã thực hiện\nĐã phân tích.\n\n## Kết quả & Tác động\nMột báo cáo duy nhất.\n\n## Mức ưu tiên\ninfo"));
    }
  }

  private sealed class ThrowingCompressor : IHermesReportCompressorService
  {
    public Task<HermesCompressedReportResult> CompressAsync(IReadOnlyList<HermesPartialReportInput> partialReports, CancellationToken cancellationToken) =>
      throw new InvalidOperationException("compress failed");
  }

  private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
  }
}
