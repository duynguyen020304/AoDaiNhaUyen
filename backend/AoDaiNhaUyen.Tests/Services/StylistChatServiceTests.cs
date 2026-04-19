using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class StylistChatServiceTests
{
  [Fact]
  public async Task AddMessageAsync_FirstTurnPersistsMemoryWithSavedAssistantMessageId()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web"
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer());

    var result = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "Mình cần áo dài đi dạy màu xanh",
      "client-1",
      [],
      CancellationToken.None);

    var storedThread = await dbContext.ChatThreads
      .Include(item => item.Memory)
      .Include(item => item.Messages)
      .SingleAsync(item => item.Id == thread.Id);

    var lastAssistantMessage = storedThread.Messages
      .Where(message => message.Role == "assistant")
      .OrderByDescending(message => message.Id)
      .First();

    Assert.NotNull(storedThread.Memory);
    Assert.Equal(lastAssistantMessage.Id, storedThread.Memory!.LastMessageId);
    Assert.True(storedThread.Memory.LastMessageId > 0);
    Assert.Equal(2, result.Messages.Count);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"stylist-chat-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
  }

  private sealed class StubIntentClassifier : IIntentClassifier
  {
    public Task<IntentClassificationDto> ClassifyAsync(
      string message,
      IReadOnlyList<ChatAttachmentDto> attachments,
      ThreadMemoryStateDto memory,
      CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new IntentClassificationDto(
        "clarification",
        "giao-vien",
        null,
        "blue",
        "lụa",
        [],
        false));
    }
  }

  private sealed class StubCatalogStylingService : ICatalogStylingService
  {
    public Task<IReadOnlyList<ChatRecommendationItemDto>> RecommendAsync(
      string? scenario,
      decimal? budgetCeiling,
      string? colorFamily,
      string? materialKeyword,
      int limit,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>([]);

    public Task<IReadOnlyList<ChatRecommendationItemDto>> LookupAsync(
      string query,
      string? scenario,
      decimal? budgetCeiling,
      string? colorFamily,
      string? materialKeyword,
      int limit,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>([]);

    public Task<IReadOnlyList<ChatRecommendationItemDto>> CompareAsync(
      IReadOnlyList<long> productIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>([]);

    public Task<IReadOnlyList<long>> ResolveProductReferencesAsync(
      string message,
      IReadOnlyList<long> shortlistedProductIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<long>>([]);
  }

  private sealed class StubCatalogTryOnService : ICatalogTryOnService
  {
    public Task<AiTryOnCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default) =>
      Task.FromResult(new AiTryOnCatalogDto([], []));

    public Task<AiTryOnResultDto> CreateAsync(
      CatalogAiTryOnRequestDto request,
      CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();
  }

  private sealed class StubStylistResponseComposer : IStylistResponseComposer
  {
    public Task<string> ComposeAsync(
      string userMessage,
      string fallbackText,
      string intent,
      string? memorySummary,
      ChatStructuredPayloadDto? structuredPayload,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(fallbackText);
  }
}
