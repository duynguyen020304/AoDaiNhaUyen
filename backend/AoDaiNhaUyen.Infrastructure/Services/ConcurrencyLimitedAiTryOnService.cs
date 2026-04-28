using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ConcurrencyLimitedAiTryOnService : IAiTryOnService, IDisposable
{
  private readonly Func<IAiTryOnService> createInner;
  private readonly SemaphoreSlim semaphore;
  private readonly ILogger<ConcurrencyLimitedAiTryOnService> logger;
  private readonly int maxConcurrentGenerations;

  public ConcurrencyLimitedAiTryOnService(
    Func<IAiTryOnService> createInner,
    IOptions<AiTryOnConcurrencyOptions> options,
    ILogger<ConcurrencyLimitedAiTryOnService> logger)
  {
    this.createInner = createInner;
    this.logger = logger;
    maxConcurrentGenerations = Math.Max(1, options.Value.MaxConcurrentGenerations);
    semaphore = new SemaphoreSlim(maxConcurrentGenerations, maxConcurrentGenerations);
  }

  public async Task<AiTryOnResultDto> GenerateAsync(
    AiTryOnRequestDto request,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation(
      "AI try-on generation waiting for slot. Available slots: {AvailableSlots}/{MaxConcurrentGenerations}",
      semaphore.CurrentCount,
      maxConcurrentGenerations);

    await semaphore.WaitAsync(cancellationToken);

    try
    {
      logger.LogInformation(
        "AI try-on generation slot acquired. Available slots: {AvailableSlots}/{MaxConcurrentGenerations}",
        semaphore.CurrentCount,
        maxConcurrentGenerations);

      return await createInner().GenerateAsync(request, cancellationToken);
    }
    finally
    {
      semaphore.Release();
      logger.LogInformation(
        "AI try-on generation slot released. Available slots: {AvailableSlots}/{MaxConcurrentGenerations}",
        semaphore.CurrentCount,
        maxConcurrentGenerations);
    }
  }

  public void Dispose()
  {
    semaphore.Dispose();
  }
}
