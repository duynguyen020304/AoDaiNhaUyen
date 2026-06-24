using System.Net;
using System.Text.Json;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

/// <summary>
/// Covers the worker's chunk-dispatch layer: parallelism bounded by MaxParallelBatches,
/// per-chunk scope isolation (no shared DbContext), and isolated failure handling. The
/// claim step (raw SQL FOR UPDATE SKIP LOCKED) is not exercised — InMemory EF has no raw
/// SQL — so tests invoke ProcessClaimedAsBatchesAsync directly with pre-claimed IDs.
/// </summary>
public sealed class BackgroundHermesOutboxWorkerTests
{
  private const string AssistantText = "Báo cáo tổng hợp cho cửa hàng.";

  [Fact]
  public async Task ProcessClaimedAsBatches_Parallel_RunsChunksConcurrently_AndCompletesAll()
  {
    // 6 events, 2 per chunk => 3 chunks; allow 3 in flight.
    var options = Options(maxParallelBatches: 3, maxBatchEvents: 2);
    var handler = new RecordingHandler(_ => Ok(AssistantText), delay: TimeSpan.FromMilliseconds(150));
    using var harness = new WorkerHarness(options, handler);

    var ids = harness.SeedProcessing(6);
    await harness.Worker.ProcessClaimedAsBatchesAsync(harness.ReadContext, ids, CancellationToken.None);

    Assert.Equal(3, handler.CallCount);                 // one Hermes call per chunk
    Assert.True(handler.MaxConcurrentCalls > 1,         // proves concurrency
      $"expected overlapping Hermes calls, saw max concurrency {handler.MaxConcurrentCalls}");
    Assert.All(handler.EventCounts, c => Assert.Equal(2, c));
    Assert.All(harness.ReloadAll(), e => Assert.Equal("completed", e.Status));
  }

  [Fact]
  public async Task ProcessClaimedAsBatches_SerialDefault_NeverOverlaps()
  {
    var options = Options(maxParallelBatches: 1, maxBatchEvents: 2);
    var handler = new RecordingHandler(_ => Ok(AssistantText), delay: TimeSpan.FromMilliseconds(50));
    using var harness = new WorkerHarness(options, handler);

    var ids = harness.SeedProcessing(6);
    await harness.Worker.ProcessClaimedAsBatchesAsync(harness.ReadContext, ids, CancellationToken.None);

    Assert.Equal(3, handler.CallCount);
    Assert.Equal(1, handler.MaxConcurrentCalls);        // strictly serial
    Assert.All(harness.ReloadAll(), e => Assert.Equal("completed", e.Status));
  }

  [Fact]
  public async Task ProcessClaimedAsBatches_OneChunkFails_SiblingsUnaffected()
  {
    // Events 0,1 carry a poison marker; the Hermes stub fails any request containing it.
    // That chunk falls back to per-event retry (also fails -> 'failed'); other 2 chunks
    // (4 events) complete normally.
    var options = Options(maxParallelBatches: 3, maxBatchEvents: 2);
    var handler = new RecordingHandler(req =>
      RequestBodyContains(req, "POISON")
        ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") }
        : Ok(AssistantText));
    using var harness = new WorkerHarness(options, handler);

    var ids = harness.SeedProcessing(6, poisonFirst: 2);
    await harness.Worker.ProcessClaimedAsBatchesAsync(harness.ReadContext, ids, CancellationToken.None);

    var all = harness.ReloadAll();
    Assert.Equal(4, all.Count(e => e.Status == "completed"));
    Assert.Equal(2, all.Count(e => e.Status == "failed"));     // poisoned chunk, no orphan completion
  }

  // ---- helpers ----

  private static HermesOutboxOptions Options(int maxParallelBatches, int maxBatchEvents) => new()
  {
    DryRun = false,
    BatchProcessingEnabled = true,
    MaxParallelBatches = maxParallelBatches,
    MaxBatchEvents = maxBatchEvents,
    MaxBatchPayloadBytes = 0
  };

  private static bool RequestBodyContains(HttpRequestMessage request, string marker)
  {
    var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
    return body.Contains(marker, StringComparison.Ordinal);
  }

  private static HttpResponseMessage Ok(string assistantText)
  {
    var body = JsonSerializer.Serialize(new
    {
      output = new[] { new { type = "message", content = new[] { new { text = assistantText } } } }
    });
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
  }

  private sealed class WorkerHarness : IDisposable
  {
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _readScope;

    public BackgroundHermesOutboxWorker Worker { get; }
    public AppDbContext ReadContext { get; }

    public WorkerHarness(HermesOutboxOptions options, HttpMessageHandler handler)
    {
      var dbName = $"hermes-worker-{Guid.NewGuid():N}";
      var services = new ServiceCollection();
      services.AddLogging();
      services.AddDbContext<AppDbContext>(o => o
        .UseInMemoryDatabase(dbName)
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
      services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler));
      services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
        new HermesAgentOptions { ApiServerUrl = "https://hermes.test", ApiServerKey = "key" }));
      services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
      services.AddScoped<IHermesEventProcessor, HermesEventProcessor>();
      _provider = services.BuildServiceProvider();

      _readScope = _provider.CreateScope();
      ReadContext = _readScope.ServiceProvider.GetRequiredService<AppDbContext>();
      Worker = new BackgroundHermesOutboxWorker(
        _provider.GetRequiredService<IServiceScopeFactory>(),
        Microsoft.Extensions.Options.Options.Create(options),
        NullLogger<BackgroundHermesOutboxWorker>.Instance);
    }

    public IReadOnlyList<Guid> SeedProcessing(int count, int poisonFirst = 0)
    {
      var now = DateTimeOffset.UtcNow;
      var list = Enumerable.Range(0, count).Select(i => new HermesEventOutbox
      {
        Id = Guid.NewGuid(),
        EventType = "checkout_completed",
        AggregateType = "Order",
        AggregateId = Guid.NewGuid().ToString("N"),
        PayloadJson = i < poisonFirst ? "{\"k\":\"POISON\"}" : "{\"k\":\"v\"}",
        Status = "processing",
        OccurredAt = now.AddSeconds(i),
        ScheduledAt = now,
        MaxAttempts = 5,
        CreatedAt = now.UtcDateTime,
        UpdatedAt = now.UtcDateTime
      }).ToList();
      ReadContext.HermesEventOutbox.AddRange(list);
      ReadContext.SaveChanges();
      return list.Select(x => x.Id).ToList();
    }

    public List<HermesEventOutbox> ReloadAll()
    {
      using var scope = _provider.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      return db.HermesEventOutbox.AsNoTracking().ToList();
    }

    public void Dispose()
    {
      _readScope.Dispose();
      _provider.Dispose();
    }
  }

  private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
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
        lock (_gate) _eventCounts.Add(ExtractEventCount(body));
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
      if (string.IsNullOrWhiteSpace(requestJson)) return 0;
      using var doc = JsonDocument.Parse(requestJson);
      if (!doc.RootElement.TryGetProperty("metadata", out var metadata)) return 0;
      return metadata.TryGetProperty("eventCount", out var count) ? count.GetInt32() : 0;
    }
  }
}
