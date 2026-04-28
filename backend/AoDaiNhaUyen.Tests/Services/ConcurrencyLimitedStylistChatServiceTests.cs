using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ConcurrencyLimitedStylistChatServiceTests
{
  [Fact]
  public async Task AddMessageAsync_LimitsConcurrentCalls()
  {
    var inner = new BlockingStylistChatService();
    using var service = CreateService(inner, 1);

    var first = service.AddMessageAsync(1, null, "guest", "first", null, []);
    await inner.WaitForStartedCallsAsync(1);

    var second = service.AddMessageAsync(1, null, "guest", "second", null, []);
    Assert.False(second.IsCompleted);

    inner.CompleteOne();
    await first;
    await inner.WaitForStartedCallsAsync(2);

    inner.CompleteOne();
    await second;
  }

  [Fact]
  public async Task AddMessageStreamAsync_EmitsQueuedEventBeforeWaiting()
  {
    var inner = new BlockingStylistChatService();
    using var service = CreateService(inner, 1);

    var first = service.AddMessageAsync(1, null, "guest", "first", null, []);
    await inner.WaitForStartedCallsAsync(1);

    await using var enumerator = service.AddMessageStreamAsync(1, null, "guest", "stream", null, []).GetAsyncEnumerator();

    Assert.True(await enumerator.MoveNextAsync());
    var queued = Assert.IsType<SseChatEvent.Queued>(enumerator.Current);
    Assert.Equal(1, queued.Position);
    Assert.Equal(1, inner.StartedCalls);

    inner.CompleteOne();
    await first;

    Assert.True(await enumerator.MoveNextAsync());
    Assert.IsType<SseChatEvent.Done>(enumerator.Current);
  }

  [Fact]
  public async Task ReadOnlyCreateAndTryOnMethods_DoNotConsumeQueueSlots()
  {
    var inner = new BlockingStylistChatService();
    using var service = CreateService(inner, 1);

    var first = service.AddMessageAsync(1, null, "guest", "first", null, []);
    await inner.WaitForStartedCallsAsync(1);

    await service.ListThreadsAsync(null, "guest");
    await service.CreateThreadAsync(null, "guest");
    await service.GetThreadAsync(1, null, "guest");
    await service.ExecuteTryOnAsync(1, null, "guest", null, []);

    inner.CompleteOne();
    await first;
  }

  private static ConcurrencyLimitedStylistChatService CreateService(
    BlockingStylistChatService inner,
    int maxConcurrentThreads)
  {
    return new ConcurrencyLimitedStylistChatService(
      () => inner,
      Options.Create(new ChatConcurrencyOptions { MaxConcurrentThreads = maxConcurrentThreads }),
      NullLogger<ConcurrencyLimitedStylistChatService>.Instance);
  }

  private sealed class BlockingStylistChatService : IStylistChatService
  {
    private readonly object sync = new();
    private readonly Queue<TaskCompletionSource<ChatThreadDetailDto>> pendingCalls = new();
    private TaskCompletionSource<bool> startedSignal = CreateSignal();

    public int StartedCalls { get; private set; }

    public Task<ChatThreadDetailDto> AddMessageAsync(
      long threadId,
      long? userId,
      string? guestKey,
      string message,
      string? clientMessageId,
      IReadOnlyList<IncomingChatAttachmentDto> attachments,
      CancellationToken cancellationToken = default)
    {
      lock (sync)
      {
        StartedCalls++;
        var completion = new TaskCompletionSource<ChatThreadDetailDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingCalls.Enqueue(completion);
        startedSignal.TrySetResult(true);
        return completion.Task;
      }
    }

    public async IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(
      long threadId,
      long? userId,
      string? guestKey,
      string message,
      string? clientMessageId,
      IReadOnlyList<IncomingChatAttachmentDto> attachments,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      lock (sync)
      {
        StartedCalls++;
        startedSignal.TrySetResult(true);
      }

      await Task.Yield();
      yield return new SseChatEvent.Done();
    }

    public Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
      long? userId,
      string? guestKey,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatThreadSummaryDto>>([]);

    public Task<ChatThreadDetailDto> CreateThreadAsync(
      long? userId,
      string? guestKey,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(CreateThread());

    public Task<ChatThreadDetailDto> GetThreadAsync(
      long threadId,
      long? userId,
      string? guestKey,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(CreateThread());

    public Task<ChatMessageDto> ExecuteTryOnAsync(
      long threadId,
      long? userId,
      string? guestKey,
      long? garmentProductId,
      IReadOnlyList<long> accessoryProductIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(new ChatMessageDto(1, "assistant", "try-on", null, DateTime.UtcNow, [], null));

    public async Task WaitForStartedCallsAsync(int expectedCalls)
    {
      while (true)
      {
        Task waitTask;
        lock (sync)
        {
          if (StartedCalls >= expectedCalls)
          {
            return;
          }

          waitTask = startedSignal.Task;
        }

        await waitTask.WaitAsync(TimeSpan.FromSeconds(5));

        lock (sync)
        {
          if (startedSignal.Task.IsCompleted)
          {
            startedSignal = CreateSignal();
          }
        }
      }
    }

    public void CompleteOne()
    {
      lock (sync)
      {
        pendingCalls.Dequeue().SetResult(CreateThread());
      }
    }

    private static ChatThreadDetailDto CreateThread() =>
      new(1, "Thread", "active", "web", DateTime.UtcNow, DateTime.UtcNow, []);

    private static TaskCompletionSource<bool> CreateSignal() =>
      new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
