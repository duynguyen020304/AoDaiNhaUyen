using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

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

    var storedMemory = new ThreadMemoryService().Read(storedThread.Memory);
    Assert.Contains(lastAssistantMessage.Content, storedMemory.RecentAssistantMessages);
  }

  [Fact]
  public async Task AddMessageAsync_FollowUpRecommendation_PrioritizesUnseenBeforeSeen()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{}",
        ResolvedRefsJsonb = "{\"shownProductIds\":[101,201]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var stylingService = new StubCatalogStylingService
    {
      GarmentRecommendations =
      [
        new ChatRecommendationItemDto(101, "Áo dài lụa", "ao-dai", "ao_dai", 1200000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(103, "Áo dài đỏ", "ao-dai", "ao_dai", 1300000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới phù hợp." ),
        new ChatRecommendationItemDto(104, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Mẫu mới khác." )
      ],
      AccessoryRecommendations =
      [
        new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(203, "Kẹp tóc", "phu-kien", "phu_kien", 120000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(202, "Băng đô", "phu-kien", "phu_kien", 150000m, null, null, null, "Phụ kiện mới." )
      ]
    };
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("outfit_recommendation", "giao-vien", null, "blue", "lụa", "ao_dai", [], false, false)),
      new ThreadMemoryService(),
      stylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "Mình cần thêm vài mẫu nữa",
      "client-follow-up",
      [],
      CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal([103, 102, 104], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal([203, 202], payload.AccessoryProducts!.Select(product => product.ProductId).ToArray());
    Assert.DoesNotContain(101, payload.GarmentProducts!.Select(product => product.ProductId));
    Assert.DoesNotContain(201, payload.AccessoryProducts!.Select(product => product.ProductId));
    Assert.Contains("Look 1", result.Messages.Last().Content);
    Assert.Contains("Khác biệt:", result.Messages.Last().Content);
    Assert.All(stylingService.RecommendExcludedProductIds, ids => Assert.Empty(ids));
  }

  [Fact]
  public async Task AddMessageAsync_BroadDifferentRequest_PrefersUnseenEvenWhenSelectedGarmentExists()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{\"selectedGarmentProductId\":101}",
        ResolvedRefsJsonb = "{\"shownProductIds\":[101,201]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var stylingService = new StubCatalogStylingService
    {
      GarmentRecommendations =
      [
        new ChatRecommendationItemDto(101, "Áo dài lụa", "ao-dai", "ao_dai", 1200000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới phù hợp."),
        new ChatRecommendationItemDto(103, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Mẫu mới khác.")
      ],
      AccessoryRecommendations =
      [
        new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đã từng gợi ý."),
        new ChatRecommendationItemDto(202, "Băng đô", "phu-kien", "phu_kien", 150000m, null, null, null, "Phụ kiện mới.")
      ]
    };
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("outfit_recommendation", "check-in", null, null, null, "ao_dai", [], false, false)),
      new ThreadMemoryService(),
      stylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "hãy gợi ý những bộ khác đi, tui ko thích lắm",
      "client-broad-different",
      [],
      CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal([102, 103, 101], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal([202, 201], payload.AccessoryProducts!.Select(product => product.ProductId).ToArray());
  }

  [Fact]
  public async Task AddMessageAsync_FollowUpRecommendation_FallsBackToSeenProducts_WhenNoUnseenRemain()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{}",
        ResolvedRefsJsonb = "{\"shownProductIds\":[101,201]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var stylingService = new StubCatalogStylingService();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("outfit_recommendation", "giao-vien", null, "blue", "lụa", "ao_dai", [], false, false)),
      new ThreadMemoryService(),
      stylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "Cho mình xem lại các mẫu phù hợp",
      "client-follow-up-repeat",
      [],
      CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal([101], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal([201], payload.AccessoryProducts!.Select(product => product.ProductId).ToArray());
    Assert.Contains("Look 1", result.Messages.Last().Content);
    Assert.Contains("Khác biệt:", result.Messages.Last().Content);
    Assert.NotNull(payload);
  }

  [Fact]
  public async Task AddMessageAsync_LookupFollowUp_KeepsSeenMatchesWhenTheyAreAllAvailableResults()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{}",
        ResolvedRefsJsonb = "{\"shownProductIds\":[101]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var stylingService = new StubCatalogStylingService
    {
      LookupResults =
      [
        new ChatRecommendationItemDto(101, "Áo dài lụa", "ao-dai", "ao_dai", 1200000m, null, null, null, "Khớp trực tiếp.")
      ]
    };
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("catalog_lookup", "giao-vien", null, "blue", "lụa", "ao_dai", [], false, false)),
      new ThreadMemoryService(),
      stylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "Cho mình xem lại mẫu áo dài lụa",
      "client-lookup-repeat",
      [],
      CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal("catalog_results", payload.Kind);
    Assert.Equal([101], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
  }

  [Fact]
  public async Task AddMessageAsync_WithAttachments_PersistsFilesUnderCanonicalUploadRoot()
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

    using var uploadRoot = new TemporaryDirectory();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(uploadRoot.Path),
      NullLogger<StylistChatService>.Instance);

    var threadDetail = await service.AddMessageAsync(
      thread.Id,
      1,
      null,
      "Đây là ảnh của mình",
      "client-attachment",
      [new IncomingChatAttachmentDto("user_image", "person.png", "image/png", [1, 2, 3, 4])],
      CancellationToken.None);

    var savedAttachment = await dbContext.ChatAttachments.SingleAsync();
    var expectedPath = Path.Combine(uploadRoot.Path, "chat", thread.Id.ToString(), Path.GetFileName(savedAttachment.FileUrl));

    Assert.Equal("/upload/chat/" + thread.Id + "/" + Path.GetFileName(expectedPath), savedAttachment.FileUrl);
    Assert.True(File.Exists(expectedPath));
    Assert.Equal([1, 2, 3, 4], await File.ReadAllBytesAsync(expectedPath));
    Assert.Contains(threadDetail.Messages.SelectMany(message => message.Attachments), attachment => attachment.Id == savedAttachment.Id);
  }

  [Fact]
  public async Task ExecuteTryOnAsync_PersistsGeneratedAttachmentAndUpdatesMemory()
  {
    await using var dbContext = CreateDbContext();
    using var uploadRoot = new TemporaryDirectory();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web"
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var personAttachment = new ChatAttachment
    {
      ThreadId = thread.Id,
      Kind = "user_image",
      FileUrl = $"/upload/chat/{thread.Id}/person.png",
      MimeType = "image/png",
      OriginalFileName = "person.png",
      FileSizeBytes = 4
    };
    dbContext.ChatAttachments.Add(personAttachment);
    await dbContext.SaveChangesAsync();

    thread.Memory = new ChatThreadMemory
    {
      ThreadId = thread.Id,
      FactsJsonb = $$"""
                    {
                      "latestPersonAttachmentId": {{personAttachment.Id}}
                    }
                    """
    };
    await dbContext.SaveChangesAsync();

    var personDirectory = Path.Combine(uploadRoot.Path, "chat", thread.Id.ToString());
    Directory.CreateDirectory(personDirectory);
    await File.WriteAllBytesAsync(Path.Combine(personDirectory, "person.png"), [8, 6, 7, 5]);

    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(new AiTryOnResultDto("data:image/png;base64,AQIDBA==", "image/png")),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(uploadRoot.Path),
      NullLogger<StylistChatService>.Instance);

    var message = await service.ExecuteTryOnAsync(
      thread.Id,
      1,
      null,
      99,
      [],
      CancellationToken.None);

    var storedThread = await dbContext.ChatThreads
      .Include(item => item.Memory)
      .Include(item => item.Attachments)
      .Include(item => item.Messages)
      .SingleAsync(item => item.Id == thread.Id);

    var tryOnAttachment = Assert.Single(storedThread.Attachments, attachment => attachment.Kind == "tryon_result");
    var absolutePath = Path.Combine(uploadRoot.Path, "chat", thread.Id.ToString(), Path.GetFileName(tryOnAttachment.FileUrl));
    var storedMemory = new ThreadMemoryService().Read(storedThread.Memory);

    Assert.True(File.Exists(absolutePath));
    Assert.Equal([1, 2, 3, 4], await File.ReadAllBytesAsync(absolutePath));
    Assert.Equal(tryOnAttachment.Id, storedMemory.LatestTryOnResultAttachmentId);
    Assert.Equal(message.Id, storedMemory.LatestTryOnResultMessageId);
    Assert.Contains(message.Attachments, attachment => attachment.Id == tryOnAttachment.Id);
  }

  [Fact]
  public async Task AddMessageStreamAsync_EmitsEventsInOrder_AndPersistsAssistantMessage()
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
      new StubStylistResponseComposer(["Xin chào ", "bạn nhé"]),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var events = await service.AddMessageStreamAsync(
      thread.Id,
      1,
      null,
      "Mình cần áo dài đi dạy",
      "client-stream-1",
      [],
      CancellationToken.None).ToListAsync();

    Assert.Collection(
      events,
      item =>
      {
        var created = Assert.IsType<SseChatEvent.Created>(item);
        Assert.Equal(0, created.MessageId);
        Assert.Equal("assistant", created.Role);
      },
      item => Assert.Equal("Xin chào ", Assert.IsType<SseChatEvent.TextDelta>(item).Delta),
      item => Assert.Equal("bạn nhé", Assert.IsType<SseChatEvent.TextDelta>(item).Delta),
      item =>
      {
        var textDone = Assert.IsType<SseChatEvent.TextDone>(item);
        Assert.Equal("Xin chào bạn nhé", textDone.FullText);
        Assert.True(textDone.MessageId > 0);
      },
      item => Assert.IsType<SseChatEvent.Done>(item));

    var storedThread = await dbContext.ChatThreads
      .Include(item => item.Memory)
      .Include(item => item.Messages)
      .SingleAsync(item => item.Id == thread.Id);
    var assistantMessage = storedThread.Messages.Single(message => message.Role == "assistant");
    var textDone = Assert.IsType<SseChatEvent.TextDone>(events[3]);

    Assert.Equal("Xin chào bạn nhé", assistantMessage.Content);
    Assert.Equal(assistantMessage.Id, textDone.MessageId);
    Assert.Equal(assistantMessage.Id, storedThread.Memory!.LastMessageId);
  }

  [Fact]
  public async Task AddMessageStreamAsync_WithAttachments_PersistsFilesUnderCanonicalUploadRoot()
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

    using var uploadRoot = new TemporaryDirectory();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(["Đã nhận ảnh của bạn"]),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(uploadRoot.Path),
      NullLogger<StylistChatService>.Instance);

    var events = await service.AddMessageStreamAsync(
      thread.Id,
      1,
      null,
      "Đây là ảnh của mình",
      "client-stream-attachment",
      [new IncomingChatAttachmentDto("user_image", "person.png", "image/png", [1, 2, 3, 4])],
      CancellationToken.None).ToListAsync();

    var created = Assert.IsType<SseChatEvent.Created>(events[0]);
    var savedAttachment = await dbContext.ChatAttachments.SingleAsync();
    var expectedPath = Path.Combine(uploadRoot.Path, "chat", thread.Id.ToString(), Path.GetFileName(savedAttachment.FileUrl));

    Assert.Equal(0, created.MessageId);
    Assert.True(File.Exists(expectedPath));
    Assert.Equal([1, 2, 3, 4], await File.ReadAllBytesAsync(expectedPath));
  }

  [Fact]
  public async Task AddMessageStreamAsync_WhenComposerFallsBack_PersistsDeterministicText()
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
      new StubStylistResponseComposer(shouldFallback: true),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var events = await service.AddMessageStreamAsync(
      thread.Id,
      1,
      null,
      "Mình cần tư vấn",
      "client-stream-fallback",
      [],
      CancellationToken.None).ToListAsync();

    var textDelta = Assert.IsType<SseChatEvent.TextDelta>(events[1]);
    var textDone = Assert.IsType<SseChatEvent.TextDone>(events[2]);
    var assistantMessage = await dbContext.ChatMessages.SingleAsync(message => message.Role == "assistant");

    Assert.Equal("fallback:clarification", textDelta.Delta);
    Assert.Equal(textDelta.Delta, textDone.FullText);
    Assert.Equal(textDone.FullText, assistantMessage.Content);
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
    private readonly IntentClassificationDto result;

    public StubIntentClassifier(IntentClassificationDto? result = null)
    {
      this.result = result ?? new IntentClassificationDto(
        "clarification",
        "giao-vien",
        null,
        "blue",
        "lụa",
        null,
        [],
        false,
        false);
    }

    public Task<IntentClassificationDto> ClassifyAsync(
      string message,
      IReadOnlyList<ChatAttachmentDto> attachments,
      ThreadMemoryStateDto memory,
      string? previousUserMessage = null,
      string? previousAssistantMessage = null,
      CancellationToken cancellationToken = default)
    {
      return Task.FromResult(this.result with { HasImageAttachments = attachments.Count > 0 });
    }
  }

  [Fact]
  public async Task AddMessageAsync_DowngradesImageAnalysisWithoutAttachments_ToClarification()
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
      new StubIntentClassifier(new IntentClassificationDto("image_style_analysis", null, null, null, null, null, [], false, false)),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "Nhìn ảnh này giúp mình", "client-2", [], CancellationToken.None);

    Assert.Equal("clarification", result.Messages.Last().Intent);
  }

  [Fact]
  public async Task AddMessageAsync_DowngradesTryOnExecuteWithoutGarment_ToClarification()
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
      new StubIntentClassifier(new IntentClassificationDto("tryon_execute", null, null, null, null, null, [], false, false)),
      new ThreadMemoryService(),
      new StubCatalogStylingService(),
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "Thử luôn cho mình", "client-3", [], CancellationToken.None);

    Assert.Equal("clarification", result.Messages.Last().Intent);
  }

  [Fact]
  public async Task AddMessageAsync_OutfitRecommendationAlwaysBuildsCombo()
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

    var catalogStylingService = new StubCatalogStylingService();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("outfit_recommendation", null, null, null, null, "phu_kien", [], false, false)),
      new ThreadMemoryService(),
      catalogStylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "Gợi ý set cho mình", "client-4", [], CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal("recommendations", payload.Kind);
    Assert.Contains("phu_kien", catalogStylingService.RecommendProductTypes);
    Assert.Contains("ao_dai", catalogStylingService.RecommendProductTypes);
    Assert.Single(payload.GarmentProducts!);
    Assert.Single(payload.AccessoryProducts!);
  }

  [Fact]
  public async Task AddMessageAsync_BuildsSetRecommendationWithAccessorySelections()
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

    var catalogStylingService = new StubCatalogStylingService();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("outfit_recommendation", "giao-vien", null, null, null, "ao_dai", [], false, false)),
      new ThreadMemoryService(),
      catalogStylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "Phối cho mình một set hoàn chỉnh", "client-5", [], CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal(101, payload.SelectedGarmentProductId);
    Assert.Equal([201], payload.SelectedAccessoryProductIds);
    Assert.Equal(["ao_dai", "phu_kien"], catalogStylingService.RecommendProductTypes);
    Assert.Equal([101], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal([201], payload.AccessoryProducts!.Select(product => product.ProductId).ToArray());
  }

  [Fact]
  public async Task AddMessageAsync_AccessoryFollowUp_ReusesGarmentShortlistAndAddsAccessorySuggestions()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{\"selectedGarmentProductId\":101}",
        ResolvedRefsJsonb = "{\"lastShortlistProductIds\":[101,102],\"lastGarmentShortlistProductIds\":[101,102]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var catalogStylingService = new StubCatalogStylingService();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("accessory_recommendation", "giao-vien", null, null, null, "phu_kien", [], false, false)),
      new ThreadMemoryService(),
      catalogStylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "vậy bạn nghĩ nó nên đi cặp như thế nào?", "client-5b", [], CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal([101, 102], payload.GarmentProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal([201], payload.AccessoryProducts!.Select(product => product.ProductId).ToArray());
    Assert.Equal(101, payload.SelectedGarmentProductId);
  }

  [Fact]
  public async Task AddMessageAsync_UsesShortlistForProductDescriptionFollowUp()
  {
    await using var dbContext = CreateDbContext();
    var thread = new ChatThread
    {
      UserId = 1,
      Status = "active",
      Source = "web",
      Memory = new ChatThreadMemory
      {
        FactsJsonb = "{}",
        ResolvedRefsJsonb = "{\"lastShortlistProductIds\":[101,102,103]}"
      }
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync();

    var catalogStylingService = new StubCatalogStylingService();
    var service = new StylistChatService(
      dbContext,
      new StubIntentClassifier(new IntentClassificationDto("product_description", null, null, null, null, null, [], false, false)),
      new ThreadMemoryService(),
      catalogStylingService,
      new StubCatalogTryOnService(),
      new StubStylistResponseComposer(),
      new StubFallbackTextService(),
      new TestUploadStoragePathResolver(),
      NullLogger<StylistChatService>.Instance);

    var result = await service.AddMessageAsync(thread.Id, 1, null, "miêu tả đặc tính của 3 áo dài này", "client-6", [], CancellationToken.None);

    var payload = result.Messages.Last().StructuredPayload!;
    Assert.Equal("comparison", payload.Kind);
    Assert.Equal([101, 102, 103], payload.Products.Select(product => product.ProductId).ToArray());
  }

  private sealed class StubFallbackTextService : IStylistFallbackTextService
  {
    public string Pick(string theme) => $"fallback:{theme}";
  }

  private sealed class StubCatalogStylingService : ICatalogStylingService
  {
    public List<string?> RecommendProductTypes { get; } = [];
    public List<IReadOnlyList<long>> RecommendExcludedProductIds { get; } = [];
    public IReadOnlyList<ChatRecommendationItemDto> GarmentRecommendations { get; set; } =
    [
      new ChatRecommendationItemDto(101, "Áo dài lụa", "ao-dai", "ao_dai", 1200000m, null, null, null, "Mẫu phù hợp với nhu cầu hiện tại.")
    ];
    public IReadOnlyList<ChatRecommendationItemDto> AccessoryRecommendations { get; set; } =
    [
      new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Hợp để phối cùng áo dài.")
    ];
    public IReadOnlyList<ChatRecommendationItemDto> LookupResults { get; set; } = [];

    public Task<IReadOnlyList<ChatRecommendationItemDto>> RecommendAsync(
      string? scenario,
      decimal? budgetCeiling,
      string? colorFamily,
      string? materialKeyword,
      string? productType,
      int limit,
      IReadOnlyList<long>? excludeProductIds = null,
      CancellationToken cancellationToken = default)
    {
      RecommendProductTypes.Add(productType);
      RecommendExcludedProductIds.Add(excludeProductIds ?? []);
      var products = productType == "phu_kien"
        ? AccessoryRecommendations
        : GarmentRecommendations;
      return Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>(products.Take(limit).ToList());
    }

    public Task<IReadOnlyList<ChatRecommendationItemDto>> LookupAsync(
      string query,
      string? scenario,
      decimal? budgetCeiling,
      string? colorFamily,
      string? materialKeyword,
      int limit,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>(LookupResults.Take(limit).ToList());

    public Task<IReadOnlyList<ChatRecommendationItemDto>> CompareAsync(
      IReadOnlyList<long> productIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatRecommendationItemDto>>(productIds
        .Select(productId => new ChatRecommendationItemDto(productId, $"Sản phẩm {productId}", "ao-dai", productId >= 200 ? "phu_kien" : "ao_dai", 1000000m, null, null, null, $"Đặc tính của sản phẩm {productId}."))
        .ToList());

    public Task<IReadOnlyList<long>> ResolveProductReferencesAsync(
      string message,
      IReadOnlyList<long> shortlistedProductIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<long>>([]);
  }

  private sealed class StubCatalogTryOnService : ICatalogTryOnService
  {
    private readonly AiTryOnResultDto result;

    public StubCatalogTryOnService(AiTryOnResultDto? result = null)
    {
      this.result = result ?? new AiTryOnResultDto("data:image/png;base64,AQID", "image/png");
    }

    public Task<AiTryOnCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default) =>
      Task.FromResult(new AiTryOnCatalogDto([], []));

    public Task<AiTryOnResultDto> CreateAsync(
      CatalogAiTryOnRequestDto request,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(result);
  }

  private sealed class StubStylistResponseComposer : IStylistResponseComposer
  {
    private readonly IReadOnlyList<string> chunks;
    private readonly bool shouldFallback;

    public StubStylistResponseComposer(IReadOnlyList<string>? chunks = null, bool shouldFallback = false)
    {
      this.chunks = chunks ?? [];
      this.shouldFallback = shouldFallback;
    }

    public Task<string> ComposeAsync(
      string userMessage,
      string fallbackText,
      string intent,
      string? memorySummary,
      ChatStructuredPayloadDto? structuredPayload,
      string? previousUserMessage = null,
      string? previousAssistantMessage = null,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(fallbackText);

    public async IAsyncEnumerable<string> ComposeStreamAsync(
      string userMessage,
      string fallbackText,
      string intent,
      string? memorySummary,
      ChatStructuredPayloadDto? structuredPayload,
      string? previousUserMessage = null,
      string? previousAssistantMessage = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      if (shouldFallback)
      {
        yield return fallbackText;
        yield break;
      }

      foreach (var chunk in chunks)
      {
        cancellationToken.ThrowIfCancellationRequested();
        yield return chunk;
        await Task.Yield();
      }
    }
  }

  private sealed class TestUploadStoragePathResolver(string? uploadRootPath = null) : IUploadStoragePathResolver
  {
    public string UploadRootPath { get; } = uploadRootPath ?? Path.Combine(Path.GetTempPath(), $"chat-upload-{Guid.NewGuid():N}");

    public string GetChatThreadDirectory(long threadId) =>
      GetAbsolutePathForRelativePath(Path.Combine("chat", threadId.ToString()));

    public string GetAbsolutePathForRelativePath(string relativePath) =>
      Path.Combine(UploadRootPath, relativePath);

    public string GetAbsolutePathForRequestPath(string requestPath)
    {
      if (!TryGetAbsolutePathForRequestPath(requestPath, out var absolutePath))
      {
        throw new InvalidOperationException();
      }

      return absolutePath;
    }

    public bool TryGetAbsolutePathForRequestPath(string requestPath, out string absolutePath)
    {
      absolutePath = string.Empty;
      if (!requestPath.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      absolutePath = Path.Combine(UploadRootPath, requestPath["/upload/".Length..].Replace('/', Path.DirectorySeparatorChar));
      return true;
    }
  }

  private sealed class TemporaryDirectory : IDisposable
  {
    public TemporaryDirectory()
    {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"aodai-chat-tests-{Guid.NewGuid():N}");
      Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
      if (Directory.Exists(Path))
      {
        Directory.Delete(Path, recursive: true);
      }
    }
  }
}
