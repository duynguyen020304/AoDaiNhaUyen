using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ConcurrencyLimitedAiTryOnServiceTests
{
  [Fact]
  public async Task GenerateAsync_LimitsConcurrentCalls()
  {
    var inner = new BlockingAiTryOnService();
    using var service = CreateService(inner, 2);

    var first = service.GenerateAsync(CreateRequest());
    var second = service.GenerateAsync(CreateRequest());
    var third = service.GenerateAsync(CreateRequest());

    await inner.WaitForStartedCallsAsync(2);

    Assert.Equal(2, inner.StartedCalls);
    Assert.False(third.IsCompleted);

    inner.CompleteOne();
    await inner.WaitForStartedCallsAsync(3);

    Assert.Equal(3, inner.StartedCalls);

    inner.CompleteAll();
    await Task.WhenAll(first, second, third);
  }

  [Fact]
  public async Task GenerateAsync_ReleasesSlotAfterSuccess()
  {
    var inner = new BlockingAiTryOnService();
    using var service = CreateService(inner, 1);

    var first = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(1);

    inner.CompleteOne();
    await first;

    var second = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(2);

    inner.CompleteOne();
    await second;
  }

  [Fact]
  public async Task GenerateAsync_ReleasesSlotAfterException()
  {
    var inner = new BlockingAiTryOnService();
    using var service = CreateService(inner, 1);

    inner.FailNext(new AiTryOnProviderException("provider failed"));

    await Assert.ThrowsAsync<AiTryOnProviderException>(() => service.GenerateAsync(CreateRequest()));

    var next = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(2);

    inner.CompleteOne();
    await next;
  }

  [Fact]
  public async Task GenerateAsync_ReleasesSlotAfterActiveCancellation()
  {
    var inner = new BlockingAiTryOnService();
    using var service = CreateService(inner, 1);

    using var activeCts = new CancellationTokenSource();
    var first = service.GenerateAsync(CreateRequest(), activeCts.Token);
    await inner.WaitForStartedCallsAsync(1);

    await activeCts.CancelAsync();
    inner.CancelOne();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

    var next = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(2);

    inner.CompleteOne();
    await next;
  }

  [Fact]
  public async Task GenerateAsync_CancelWhileWaitingDoesNotConsumeSlot()
  {
    var inner = new BlockingAiTryOnService();
    using var service = CreateService(inner, 1);

    var first = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(1);

    using var waitingCts = new CancellationTokenSource();
    var waiting = service.GenerateAsync(CreateRequest(), waitingCts.Token);

    await waitingCts.CancelAsync();

    await Assert.ThrowsAsync<OperationCanceledException>(() => waiting);
    Assert.Equal(1, inner.StartedCalls);

    inner.CompleteOne();
    await first;

    var next = service.GenerateAsync(CreateRequest());
    await inner.WaitForStartedCallsAsync(2);

    inner.CompleteOne();
    await next;
  }

  private static ConcurrencyLimitedAiTryOnService CreateService(
    IAiTryOnService inner,
    int maxConcurrentGenerations) =>
    new(
      () => inner,
      Options.Create(new AiTryOnConcurrencyOptions
      {
        MaxConcurrentGenerations = maxConcurrentGenerations
      }),
      NullLogger<ConcurrencyLimitedAiTryOnService>.Instance);

  private static AiTryOnRequestDto CreateRequest() =>
    new(
      "garment-1",
      [1, 2, 3],
      "image/png",
      [4, 5, 6],
      "image/png",
      []);

  private sealed class BlockingAiTryOnService : IAiTryOnService
  {
    private readonly object sync = new();
    private readonly Queue<TaskCompletionSource<AiTryOnResultDto>> pendingCalls = new();
    private readonly Queue<Exception> failures = new();
    private TaskCompletionSource<bool> startedSignal = CreateSignal();

    public int StartedCalls { get; private set; }

    public Task<AiTryOnResultDto> GenerateAsync(
      AiTryOnRequestDto request,
      CancellationToken cancellationToken = default)
    {
      lock (sync)
      {
        StartedCalls++;

        if (failures.Count > 0)
        {
          var failure = failures.Dequeue();
          startedSignal.TrySetResult(true);
          return Task.FromException<AiTryOnResultDto>(failure);
        }

        var completion = new TaskCompletionSource<AiTryOnResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingCalls.Enqueue(completion);
        startedSignal.TrySetResult(true);
        return completion.Task;
      }
    }

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
        pendingCalls.Dequeue().SetResult(new AiTryOnResultDto("data:image/png;base64,result", "image/png"));
      }
    }

    public void CompleteAll()
    {
      lock (sync)
      {
        while (pendingCalls.Count > 0)
        {
          pendingCalls.Dequeue().SetResult(new AiTryOnResultDto("data:image/png;base64,result", "image/png"));
        }
      }
    }

    public void CancelOne()
    {
      lock (sync)
      {
        pendingCalls.Dequeue().SetCanceled();
      }
    }

    public void FailNext(Exception exception)
    {
      lock (sync)
      {
        failures.Enqueue(exception);
      }
    }

    private static TaskCompletionSource<bool> CreateSignal() =>
      new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
