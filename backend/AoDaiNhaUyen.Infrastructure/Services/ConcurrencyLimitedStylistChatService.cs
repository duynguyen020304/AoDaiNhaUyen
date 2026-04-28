using System.Runtime.CompilerServices;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ConcurrencyLimitedStylistChatService : IStylistChatService, IDisposable
{
  private readonly Func<(IStylistChatService Inner, IDisposable? Scope)> createInner;
  private readonly SemaphoreSlim semaphore;
  private readonly ILogger<ConcurrencyLimitedStylistChatService> logger;
  private readonly object queueSync = new();
  private readonly int maxConcurrentThreads;
  private int waitingCount;

  public ConcurrencyLimitedStylistChatService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ChatConcurrencyOptions> options,
    ILogger<ConcurrencyLimitedStylistChatService> logger) : this(
      () =>
      {
        var scope = serviceScopeFactory.CreateScope();
        return (scope.ServiceProvider.GetRequiredService<StylistChatService>(), scope);
      },
      options,
      logger)
  {
  }

  public ConcurrencyLimitedStylistChatService(
    Func<IStylistChatService> createInner,
    IOptions<ChatConcurrencyOptions> options,
    ILogger<ConcurrencyLimitedStylistChatService> logger) : this(
      () => (createInner(), null),
      options,
      logger)
  {
  }

  private ConcurrencyLimitedStylistChatService(
    Func<(IStylistChatService Inner, IDisposable? Scope)> createInner,
    IOptions<ChatConcurrencyOptions> options,
    ILogger<ConcurrencyLimitedStylistChatService> logger)
  {
    this.createInner = createInner;
    this.logger = logger;
    maxConcurrentThreads = Math.Max(1, options.Value.MaxConcurrentThreads);
    semaphore = new SemaphoreSlim(maxConcurrentThreads, maxConcurrentThreads);
  }

  public async Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    var inner = createInner();
    using var scope = inner.Scope;
    return await inner.Inner.ListThreadsAsync(userId, guestKey, cancellationToken);
  }

  public async Task<ChatThreadDetailDto> CreateThreadAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    var inner = createInner();
    using var scope = inner.Scope;
    return await inner.Inner.CreateThreadAsync(userId, guestKey, cancellationToken);
  }

  public async Task<ChatThreadDetailDto> GetThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    var inner = createInner();
    using var scope = inner.Scope;
    return await inner.Inner.GetThreadAsync(threadId, userId, guestKey, cancellationToken);
  }

  public async Task<ChatThreadDetailDto> AddMessageAsync(
    long threadId,
    long? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken = default)
  {
    await WaitForSlotAsync(cancellationToken);
    try
    {
      var inner = createInner();
      using var scope = inner.Scope;
      return await inner.Inner.AddMessageAsync(
        threadId,
        userId,
        guestKey,
        message,
        clientMessageId,
        attachments,
        cancellationToken);
    }
    finally
    {
      ReleaseSlot();
    }
  }

  public async IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(
    long threadId,
    long? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var position = GetQueuePosition();
    yield return new SseChatEvent.Queued(position);

    await WaitForSlotAsync(cancellationToken);
    var inner = createInner();
    using var scope = inner.Scope;
    try
    {
      await foreach (var sseEvent in inner.Inner.AddMessageStreamAsync(
        threadId,
        userId,
        guestKey,
        message,
        clientMessageId,
        attachments,
        cancellationToken).WithCancellation(cancellationToken))
      {
        yield return sseEvent;
      }
    }
    finally
    {
      ReleaseSlot();
    }
  }

  public async Task<ChatMessageDto> ExecuteTryOnAsync(
    long threadId,
    long? userId,
    string? guestKey,
    long? garmentProductId,
    IReadOnlyList<long> accessoryProductIds,
    CancellationToken cancellationToken = default)
  {
    var inner = createInner();
    using var scope = inner.Scope;
    return await inner.Inner.ExecuteTryOnAsync(
      threadId,
      userId,
      guestKey,
      garmentProductId,
      accessoryProductIds,
      cancellationToken);
  }

  public void Dispose()
  {
    semaphore.Dispose();
  }

  private int GetQueuePosition()
  {
    lock (queueSync)
    {
      return semaphore.CurrentCount > 0 ? 0 : waitingCount + 1;
    }
  }

  private async Task WaitForSlotAsync(CancellationToken cancellationToken)
  {
    var countedAsWaiting = false;
    lock (queueSync)
    {
      if (semaphore.CurrentCount == 0)
      {
        waitingCount++;
        countedAsWaiting = true;
      }
    }

    logger.LogInformation(
      "Stylist chat message waiting for AI slot. Available slots: {AvailableSlots}/{MaxConcurrentThreads}",
      semaphore.CurrentCount,
      maxConcurrentThreads);

    try
    {
      await semaphore.WaitAsync(cancellationToken);
    }
    finally
    {
      if (countedAsWaiting)
      {
        lock (queueSync)
        {
          waitingCount = Math.Max(0, waitingCount - 1);
        }
      }
    }

    logger.LogInformation(
      "Stylist chat message AI slot acquired. Available slots: {AvailableSlots}/{MaxConcurrentThreads}",
      semaphore.CurrentCount,
      maxConcurrentThreads);
  }

  private void ReleaseSlot()
  {
    semaphore.Release();
    logger.LogInformation(
      "Stylist chat message AI slot released. Available slots: {AvailableSlots}/{MaxConcurrentThreads}",
      semaphore.CurrentCount,
      maxConcurrentThreads);
  }
}
