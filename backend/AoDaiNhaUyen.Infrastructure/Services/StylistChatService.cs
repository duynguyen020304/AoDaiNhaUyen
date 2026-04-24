using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class StylistChatService(
  AppDbContext dbContext,
  IIntentClassifier intentClassifier,
  IThreadMemoryService threadMemoryService,
  ICatalogStylingService catalogStylingService,
  ICatalogTryOnService catalogTryOnService,
  IStylistResponseComposer stylistResponseComposer,
  IStylistFallbackTextService fallbackTextService,
  IUploadStoragePathResolver uploadStoragePathResolver,
  ILogger<StylistChatService> logger) : IStylistChatService
{
  private const string AssistantRole = "assistant";
  private const string UserRole = "user";
  private const string PromptVersion = "deterministic-stylist-v1";
  private const int RecommendationLookCount = 3;
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);

    var threads = await BuildOwnedThreadQuery(userId, HashGuestKey(guestKey))
      .Include(thread => thread.Messages)
      .OrderByDescending(thread => thread.UpdatedAt)
      .ToListAsync(cancellationToken);

    return threads.Select(MapThreadSummary).ToList();
  }

  public async Task<ChatThreadDetailDto> CreateThreadAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);

    var thread = new ChatThread
    {
      UserId = userId,
      GuestKeyHash = userId.HasValue ? null : HashGuestKey(guestKey),
      Status = "active",
      Source = "web"
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync(cancellationToken);

    dbContext.ChatMessages.Add(new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = fallbackTextService.Pick("thread_welcome"),
      Intent = "clarification",
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    });
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return await GetThreadAsync(thread.Id, userId, guestKey, cancellationToken);
  }

  public async Task<ChatThreadDetailDto> GetThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    return MapThreadDetail(thread);
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
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var userMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = UserRole,
      Content = message.Trim(),
      ClientMessageId = clientMessageId,
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    };
    dbContext.ChatMessages.Add(userMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    var savedAttachments = await SaveIncomingAttachmentsAsync(thread.Id, userMessage.Id, attachments, cancellationToken);
    threadMemoryService.ApplyUserTurn(memory, savedAttachments);
    threadMemoryService.ApplyUserConversationTurn(memory, userMessage.Content);
    var previousAssistantMessage = memory.RecentAssistantMessages.LastOrDefault();
    var previousUserMessage = memory.RecentUserMessages.Count > 1
      ? memory.RecentUserMessages[^2]
      : null;

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, classification, memory, cancellationToken);
    classification = NormalizeClassification(classification with
    {
      ReferencedProductIds = referencedProductIds,
      RetrievalQuery = string.IsNullOrWhiteSpace(classification.RetrievalQuery) ? userMessage.Content : classification.RetrievalQuery,
      NeedsCatalogLookup = classification.NeedsCatalogLookup || referencedProductIds.Count > 0
    }, memory, savedAttachments.Count > 0);
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(userMessage.Content, classification, memory, [.. thread.Attachments], savedAttachments, cancellationToken);
    var imageCatalogText = BuildImageCatalogText(memory);
    var composedContent = await stylistResponseComposer.ComposeAsync(
      userMessage.Content,
      assistantTurn.Content,
      classification.Intent,
      thread.Memory?.Summary,
      assistantTurn.StructuredPayload,
      previousUserMessage,
      previousAssistantMessage,
      assistantTurn.ReferencedImages,
      imageCatalogText,
      cancellationToken);
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = composedContent,
      Intent = classification.Intent,
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = assistantTurn.StructuredPayload is null
        ? null
        : JsonSerializer.Serialize(assistantTurn.StructuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    threadMemoryService.ApplyAssistantTurn(memory, classification, assistantTurn.StructuredPayload, null, null, assistantMessage.Content);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return await GetThreadAsync(thread.Id, userId, guestKey, cancellationToken);
  }

  public async Task<ChatMessageDto> ExecuteTryOnAsync(
    long threadId,
    long? userId,
    string? guestKey,
    long? garmentProductId,
    IReadOnlyList<long> accessoryProductIds,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var personAttachmentId = memory.LatestPersonAttachmentId;
    if (!personAttachmentId.HasValue)
    {
      throw new InvalidOperationException("Bạn cần gửi ảnh người mặc trước khi thử đồ.");
    }

    var personAttachment = thread.Attachments.FirstOrDefault(attachment => attachment.Id == personAttachmentId.Value);
    if (personAttachment is null)
    {
      throw new InvalidOperationException("Không tìm thấy ảnh người mặc gần nhất.");
    }

    var selectedGarment = garmentProductId ?? memory.SelectedGarmentProductId;
    if (!selectedGarment.HasValue)
    {
      throw new InvalidOperationException("Bạn cần chọn một mẫu áo dài trước khi thử đồ.");
    }

    var personBytes = await ReadStoredAttachmentBytesAsync(personAttachment.FileUrl, cancellationToken);
    var result = await catalogTryOnService.CreateAsync(
      new CatalogAiTryOnRequestDto(
        null,
        personBytes,
        personAttachment.MimeType,
        selectedGarment.Value,
        null,
        accessoryProductIds.Count > 0 ? accessoryProductIds : memory.SelectedAccessoryProductIds,
        null,
        null,
        []),
      cancellationToken);

    var generatedAttachment = await SaveGeneratedTryOnAttachmentAsync(thread.Id, result, cancellationToken);
    var structuredPayload = new ChatStructuredPayloadDto(
      "tryon_result",
      memory.Scenario,
      false,
      false,
      selectedGarment,
      accessoryProductIds.Count > 0 ? accessoryProductIds.ToList() : memory.SelectedAccessoryProductIds,
      [],
      [],
      [],
      []);

    var fallbackContent = fallbackTextService.Pick("tryon_result");
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = await stylistResponseComposer.ComposeAsync(
        "Tạo ảnh thử đồ cho mình",
        fallbackContent,
        "tryon_execute",
        thread.Memory?.Summary,
        structuredPayload,
        memory.RecentUserMessages.LastOrDefault(),
        memory.RecentAssistantMessages.LastOrDefault(),
        null,
        null,
        cancellationToken),
      Intent = "tryon_execute",
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = JsonSerializer.Serialize(structuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    generatedAttachment.MessageId = assistantMessage.Id;
    threadMemoryService.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("tryon_execute", memory.Scenario, memory.BudgetCeiling, memory.ColorFamily, memory.MaterialKeyword, "ao_dai", [], false),
      structuredPayload,
      generatedAttachment.Id,
      assistantMessage.Id,
      assistantMessage.Content);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return MapMessage(assistantMessage, [generatedAttachment]);
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
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var userMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = UserRole,
      Content = message.Trim(),
      ClientMessageId = clientMessageId,
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    };
    dbContext.ChatMessages.Add(userMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    var savedAttachments = await SaveIncomingAttachmentsAsync(thread.Id, userMessage.Id, attachments, cancellationToken);
    threadMemoryService.ApplyUserTurn(memory, savedAttachments);
    threadMemoryService.ApplyUserConversationTurn(memory, userMessage.Content);
    var previousAssistantMessage = memory.RecentAssistantMessages.LastOrDefault();
    var previousUserMessage = memory.RecentUserMessages.Count > 1
      ? memory.RecentUserMessages[^2]
      : null;

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, classification, memory, cancellationToken);
    classification = NormalizeClassification(classification with
    {
      ReferencedProductIds = referencedProductIds,
      RetrievalQuery = string.IsNullOrWhiteSpace(classification.RetrievalQuery) ? userMessage.Content : classification.RetrievalQuery,
      NeedsCatalogLookup = classification.NeedsCatalogLookup || referencedProductIds.Count > 0
    }, memory, savedAttachments.Count > 0);
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(userMessage.Content, classification, memory, [.. thread.Attachments], savedAttachments, cancellationToken);
    var imageCatalogText = BuildImageCatalogText(memory);

    yield return new SseChatEvent.Created(
      0,
      AssistantRole,
      "",
      classification.Intent,
      DateTime.UtcNow,
      [],
      assistantTurn.StructuredPayload);

    var accumulatedText = new StringBuilder();

    await foreach (var chunk in stylistResponseComposer.ComposeStreamAsync(
      userMessage.Content,
      assistantTurn.Content,
      classification.Intent,
      thread.Memory?.Summary,
      assistantTurn.StructuredPayload,
      previousUserMessage,
      previousAssistantMessage,
      assistantTurn.ReferencedImages,
      imageCatalogText,
      cancellationToken).WithCancellation(cancellationToken))
    {
      accumulatedText.Append(chunk);
      yield return new SseChatEvent.TextDelta(chunk);
    }

    var finalText = accumulatedText.Length > 0 ? accumulatedText.ToString() : assistantTurn.Content;
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = finalText,
      Intent = classification.Intent,
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = assistantTurn.StructuredPayload is null
        ? null
        : JsonSerializer.Serialize(assistantTurn.StructuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    threadMemoryService.ApplyAssistantTurn(memory, classification, assistantTurn.StructuredPayload, null, null, assistantMessage.Content);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    yield return new SseChatEvent.TextDone(finalText, assistantMessage.Id, assistantMessage.CreatedAt);
    yield return new SseChatEvent.Done();
  }

  private async Task ClaimGuestThreadsAsync(long? userId, string? guestKey, CancellationToken cancellationToken)
  {
    if (!userId.HasValue || string.IsNullOrWhiteSpace(guestKey))
    {
      return;
    }

    var guestHash = HashGuestKey(guestKey);
    var guestThreads = await dbContext.ChatThreads
      .Where(thread => thread.GuestKeyHash == guestHash && thread.UserId == null)
      .ToListAsync(cancellationToken);

    if (guestThreads.Count == 0)
    {
      return;
    }

    foreach (var thread in guestThreads)
    {
      thread.UserId = userId.Value;
      thread.GuestKeyHash = null;
      thread.ClaimedAt = DateTime.UtcNow;
      thread.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private IQueryable<ChatThread> BuildOwnedThreadQuery(long? userId, string? guestHash)
  {
    return dbContext.ChatThreads.AsQueryable().Where(thread =>
      userId.HasValue ? thread.UserId == userId.Value : thread.GuestKeyHash == guestHash);
  }

  private async Task<ChatThread> LoadOwnedThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken)
  {
    var thread = await BuildOwnedThreadQuery(userId, HashGuestKey(guestKey))
      .Include(item => item.Messages.OrderBy(message => message.CreatedAt))
      .Include(item => item.Attachments.OrderBy(attachment => attachment.CreatedAt))
      .Include(item => item.Memory)
      .FirstOrDefaultAsync(item => item.Id == threadId, cancellationToken);

    if (thread is null)
    {
      throw new InvalidOperationException("Không tìm thấy cuộc trò chuyện.");
    }

    return thread;
  }

  private async Task<IReadOnlyList<long>> ResolveReferencedProductIdsAsync(
    string message,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var shortlist = memory.ShortlistedProductIds.Count > 0 ? memory.ShortlistedProductIds : memory.GarmentShortlistedProductIds;
    var references = await catalogStylingService.ResolveProductReferencesAsync(
      message,
      shortlist,
      cancellationToken);

    if (references.Count > 0)
    {
      return references;
    }

    return classification.ProductReferenceScope switch
    {
      "selected_garment" when memory.SelectedGarmentProductId.HasValue => [memory.SelectedGarmentProductId.Value],
      "shortlist_top_3" when memory.ShortlistedProductIds.Count > 0 => memory.ShortlistedProductIds.Take(3).ToList(),
      "shortlist_all" when memory.ShortlistedProductIds.Count > 0 => memory.ShortlistedProductIds.ToList(),
      _ => []
    };
  }

  private async Task<AssistantTurn> BuildAssistantTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    IReadOnlyList<ChatAttachment> threadAttachments,
    IReadOnlyList<ChatAttachment> currentTurnAttachments,
    CancellationToken cancellationToken)
  {
    return classification.Intent switch
    {
      "catalog_lookup" => await BuildLookupTurnAsync(userMessage, classification, memory, cancellationToken),
      "outfit_recommendation" => await BuildRecommendationTurnAsync(userMessage, classification, memory, cancellationToken),
      "accessory_recommendation" => await BuildAccessoryRecommendationTurnAsync(userMessage, classification, memory, cancellationToken),
      "product_description" => await BuildProductDescriptionTurnAsync(classification, memory, cancellationToken),
      "product_comparison" => await BuildComparisonTurnAsync(classification, cancellationToken),
      "tryon_prepare" or "tryon_execute" => await BuildTryOnTurnAsync(classification, memory, cancellationToken),
      "image_style_analysis" => await BuildImageAnalysisTurnAsync(userMessage, classification, memory, threadAttachments, currentTurnAttachments, cancellationToken),
      "out_of_scope" => new AssistantTurn(
        fallbackTextService.Pick("out_of_scope"),
        null),
      _ => new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null)
    };
  }

  private static IntentClassificationDto NormalizeClassification(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    bool hasImageAttachments)
  {
    var hasPersonImage = hasImageAttachments || memory.LatestPersonAttachmentId.HasValue;
    var hasGarmentReference = classification.ReferencedProductIds.Count > 0 || memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0;

    var normalizedIntent = classification.Intent switch
    {
      "tryon_execute" when !hasGarmentReference => "clarification",
      "tryon_execute" when !hasPersonImage => "tryon_prepare",
      "tryon_prepare" when !hasGarmentReference => "clarification",
      "product_comparison" when classification.ReferencedProductIds.Count < 2 => "clarification",
      "image_style_analysis" when !hasImageAttachments && memory.ImageCatalog.Count == 0 => "clarification",
      "catalog_lookup" or "outfit_recommendation" or "accessory_recommendation" or "product_description" or "product_comparison" or "tryon_prepare" or "tryon_execute" or "image_style_analysis" or "clarification" or "out_of_scope" => classification.Intent,
      _ => "clarification"
    };

    return classification with
    {
      Intent = normalizedIntent,
      RequiresPersonImage = normalizedIntent is "tryon_prepare" or "tryon_execute" && !hasPersonImage,
      HasImageAttachments = hasImageAttachments
    };
  }

  private async Task<AssistantTurn> BuildLookupTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var query = BuildCatalogLookupQuery(userMessage, classification);
    var preferUnseen = ShouldPreferUnseenAlternatives(userMessage, classification, memory);

    var products = await catalogStylingService.LookupAsync(
      query,
      classification.Scenario,
      classification.BudgetCeiling,
      classification.ColorFamily,
      classification.MaterialKeyword,
      classification.ProductType,
      8,
      cancellationToken);

    var selectedProducts = SelectProducts(products, memory, preferUnseen, 4);

    logger.LogInformation(
      "Catalog lookup intent query={Query} source={QuerySource} totalCandidates={TotalCandidates} selected={Selected} preferUnseen={PreferUnseen}",
      query,
      ResolveCatalogLookupQuerySource(userMessage, classification),
      products.Count,
      selectedProducts.Count,
      preferUnseen);

    if (selectedProducts.Count == 0)
    {
      logger.LogInformation(
        "Catalog lookup empty reason={EmptyReason} query={Query} totalCandidates={TotalCandidates}",
        products.Count == 0 ? "lookup_zero" : "select_zero",
        query,
        products.Count);

      return new AssistantTurn(
        fallbackTextService.Pick("catalog_lookup_empty"),
        null);
    }

    var garmentProducts = selectedProducts.Where(product => product.ProductType == "ao_dai").ToList();
    var accessoryProducts = selectedProducts.Where(product => product.ProductType == "phu_kien").ToList();

    if (string.Equals(classification.ProductType, "phu_kien", StringComparison.OrdinalIgnoreCase) && accessoryProducts.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("catalog_lookup_empty_for_type", ("productType", "phụ kiện")),
        null);
    }

    if (string.Equals(classification.ProductType, "ao_dai", StringComparison.OrdinalIgnoreCase) && garmentProducts.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("catalog_lookup_empty_for_type", ("productType", "áo dài")),
        null);
    }

    return new AssistantTurn(
      fallbackTextService.Pick("catalog_lookup_intro", ("count", selectedProducts.Count.ToString())),
      BuildStructuredPayload(
        "catalog_results",
        classification.Scenario,
        false,
        false,
        memory.SelectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        [],
        garmentProducts,
        accessoryProducts));
  }

  private static string BuildCatalogLookupQuery(string userMessage, IntentClassificationDto classification)
  {
    var trimmedMessage = userMessage?.Trim();
    if (!string.IsNullOrWhiteSpace(trimmedMessage))
    {
      var normalizedUserMessage = ChatTextUtils.Normalize(trimmedMessage);
      if (classification.ProductType == "ao_dai" && !normalizedUserMessage.Contains("ao dai"))
      {
        return $"ao dai {trimmedMessage}";
      }

      if (classification.ProductType == "phu_kien" && !normalizedUserMessage.Contains("phu kien"))
      {
        return $"phu kien {trimmedMessage}";
      }
    }

    if (!string.IsNullOrWhiteSpace(classification.RetrievalQuery) && !LooksMachineGenerated(classification.RetrievalQuery))
    {
      return classification.RetrievalQuery.Trim();
    }

    if (!string.IsNullOrWhiteSpace(trimmedMessage))
    {
      return trimmedMessage;
    }

    return string.Join(' ', new[]
    {
      classification.ProductType == "phu_kien" ? "phu kien" : "ao dai",
      classification.ColorFamily,
      classification.MaterialKeyword,
      classification.Scenario?.Replace('-', ' ')
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
  }

  private static string ResolveCatalogLookupQuerySource(string userMessage, IntentClassificationDto classification)
  {
    if (!string.IsNullOrWhiteSpace(classification.RetrievalQuery) && !LooksMachineGenerated(classification.RetrievalQuery))
    {
      return "classifier";
    }

    return !string.IsNullOrWhiteSpace(userMessage) ? "userMessage" : "slotFallback";
  }

  private static bool LooksMachineGenerated(string query)
  {
    var normalized = ChatTextUtils.Normalize(query);
    return normalized == "ao dai" || normalized == "phu kien" || normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 2;
  }

  private async Task<AssistantTurn> BuildRecommendationTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    var preferUnseenAlternatives = string.Equals(classification.SelectionStrategy, "novelty_first", StringComparison.OrdinalIgnoreCase)
      || ShouldPreferUnseenAlternatives(userMessage, classification, memory);
    var rejectedProductIds = memory.RejectedProductIds.Count > 0 ? memory.RejectedProductIds : [];
    var garmentFetchLimit = preferUnseenAlternatives
      ? Math.Max(RecommendationLookCount * 4, RecommendationLookCount + memory.ShownProductIds.Count + rejectedProductIds.Count)
      : RecommendationLookCount;

    var garmentCandidates = await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "ao_dai",
      garmentFetchLimit,
      rejectedProductIds,
      cancellationToken);
    var garmentProducts = SelectProducts(garmentCandidates, memory, preferUnseenAlternatives, RecommendationLookCount);
    if (garmentProducts.Count == 0 && preferUnseenAlternatives)
    {
      garmentProducts = SelectProducts(garmentCandidates, memory, false, RecommendationLookCount);
    }

    if (garmentProducts.Count == 0)
    {
      logger.LogInformation(
        "Chat recommendations exhausted for scenario {Scenario}. preferUnseen={PreferUnseen} shownCount={ShownCount} rejectedCount={RejectedCount}",
        scenario,
        preferUnseenAlternatives,
        memory.ShownProductIds.Count,
        rejectedProductIds.Count);

      return new AssistantTurn(
        preferUnseenAlternatives
          ? BuildExhaustedRecommendationCopy(scenario)
          : fallbackTextService.Pick("recommendation_empty"),
        null);
    }

    var selectedGarment = garmentProducts[0].ProductId;
    var accessoryFetchLimit = Math.Max(RecommendationLookCount * 3, RecommendationLookCount + memory.ShownProductIds.Count + rejectedProductIds.Count);
    var accessoryCandidates = await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "phu_kien",
      accessoryFetchLimit,
      rejectedProductIds,
      cancellationToken);
    var accessoryProducts = SelectProducts(
      accessoryCandidates,
      memory,
      preferUnseenAlternatives || memory.AccessoryShortlistedProductIds.Count > 0,
      RecommendationLookCount - 1,
      memory.AccessoryShortlistedProductIds,
      BuildOutfitSignatureSet(memory),
      selectedGarment,
      allowSeenFallback: true);

    var selectedAccessoryProductIds = accessoryProducts.Select(product => product.ProductId).ToList();
    var combinedProducts = garmentProducts.Concat(accessoryProducts).ToList();
    var recommendationCopy = BuildRecommendationCopy(garmentProducts, accessoryProducts, scenario, preferUnseenAlternatives);

    logger.LogInformation(
      "Chat recommendations selected for scenario {Scenario}. strategy={Strategy} garments={GarmentIds} accessories={AccessoryIds}",
      scenario,
      preferUnseenAlternatives ? "novelty_first" : "relevance_first",
      string.Join(",", garmentProducts.Select(product => product.ProductId)),
      string.Join(",", accessoryProducts.Select(product => product.ProductId)));

    return new AssistantTurn(
      recommendationCopy,
      BuildStructuredPayload(
        "recommendations",
        scenario,
        false,
        false,
        selectedGarment,
        selectedAccessoryProductIds,
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        garmentProducts,
        accessoryProducts,
        combinedProducts));
  }

  private async Task<AssistantTurn> BuildAccessoryRecommendationTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    var rawAccessoryProducts = classification.HasSpecificAccessoryRequest
      ? await catalogStylingService.LookupAsync(
        userMessage,
        scenario,
        classification.BudgetCeiling ?? memory.BudgetCeiling,
        classification.ColorFamily ?? memory.ColorFamily,
        classification.MaterialKeyword ?? memory.MaterialKeyword,
        "phu_kien",
        RecommendationLookCount * 3,
        cancellationToken)
      : await catalogStylingService.RecommendAsync(
        scenario,
        classification.BudgetCeiling ?? memory.BudgetCeiling,
        classification.ColorFamily ?? memory.ColorFamily,
        classification.MaterialKeyword ?? memory.MaterialKeyword,
        "phu_kien",
        RecommendationLookCount * 2,
        memory.RejectedProductIds,
        cancellationToken);
    var accessoryProducts = SelectProducts(
      rawAccessoryProducts,
      memory,
      true,
      RecommendationLookCount,
      memory.AccessoryShortlistedProductIds,
      BuildOutfitSignatureSet(memory));

    if (accessoryProducts.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("recommendation_empty"),
        null);
    }

    var shouldBuildCombo = ShouldBuildAccessoryCombo(memory, classification);
    var garmentProducts = shouldBuildCombo
      ? await LoadComboGarmentProductsAsync(classification, memory, cancellationToken)
      : [];
    var selectedGarmentProductId = garmentProducts.FirstOrDefault()?.ProductId ?? memory.SelectedGarmentProductId;
    var combinedProducts = garmentProducts.Concat(accessoryProducts).ToList();

    return new AssistantTurn(
      shouldBuildCombo
        ? BuildRecommendationCopy(garmentProducts, accessoryProducts, scenario, true)
        : fallbackTextService.Pick("accessory_recommendation_intro"),
      BuildStructuredPayload(
        "recommendations",
        scenario,
        false,
        false,
        selectedGarmentProductId,
        accessoryProducts.Select(product => product.ProductId).ToList(),
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        garmentProducts,
        accessoryProducts,
        shouldBuildCombo ? combinedProducts : accessoryProducts));
  }

  private async Task<AssistantTurn> BuildProductDescriptionTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var productIds = classification.ReferencedProductIds.Count > 0
      ? classification.ReferencedProductIds
      : memory.ShortlistedProductIds.Take(3).ToList();

    if (productIds.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null);
    }

    var products = await catalogStylingService.CompareAsync(productIds, cancellationToken);
    if (products.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null);
    }

    var description = string.Join(
      "\n",
      products.Select((product, index) => $"{index + 1}. {product.Name}: {product.Rationale}"));

    return new AssistantTurn(
      fallbackTextService.Pick("product_description_intro", ("description", description)),
      BuildStructuredPayload(
        "comparison",
        classification.Scenario ?? memory.Scenario,
        false,
        false,
        memory.SelectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        [],
        products,
        []));
  }

  private async Task<AssistantTurn> BuildComparisonTurnAsync(
    IntentClassificationDto classification,
    CancellationToken cancellationToken)
  {
    if (classification.ReferencedProductIds.Count < 2)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("comparison_need_more_refs"),
        null);
    }

    var products = await catalogStylingService.CompareAsync(classification.ReferencedProductIds.Take(2).ToList(), cancellationToken);
    if (products.Count < 2)
    {
      return new AssistantTurn(fallbackTextService.Pick("comparison_insufficient_data"), null);
    }

    return new AssistantTurn(
      fallbackTextService.Pick(
        "comparison_result",
        ("leftName", products[0].Name),
        ("leftRationale", products[0].Rationale),
        ("rightName", products[1].Name),
        ("rightRationale", products[1].Rationale),
        ("suggestedName", products[0].Name)),
      BuildStructuredPayload(
        "comparison",
        classification.Scenario,
        false,
        false,
        null,
        [],
        [],
        products,
        []));
  }

  private async Task<AssistantTurn> BuildTryOnTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var selectedGarmentProductId = classification.ReferencedProductIds.FirstOrDefault();
    if (selectedGarmentProductId == 0)
    {
      selectedGarmentProductId = memory.SelectedGarmentProductId ?? memory.ShortlistedProductIds.FirstOrDefault();
    }

    if (selectedGarmentProductId == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("tryon_need_garment"),
        null);
    }

    var products = await catalogStylingService.CompareAsync([selectedGarmentProductId], cancellationToken);
    var personAttachmentId = ResolveReferencedPersonImageId(classification, memory);
    var requiresPersonImage = !personAttachmentId.HasValue;
    var pending = requiresPersonImage ? new List<string> { "upload_person_image" } : [];

    return new AssistantTurn(
      requiresPersonImage
        ? fallbackTextService.Pick("tryon_need_person_image")
        : fallbackTextService.Pick("tryon_ready"),
      BuildStructuredPayload(
        "tryon_ready",
        classification.Scenario ?? memory.Scenario,
        !requiresPersonImage,
        requiresPersonImage,
        selectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        pending,
        products,
        []));
  }

  private async Task<AssistantTurn> BuildImageAnalysisTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    IReadOnlyList<ChatAttachment> threadAttachments,
    IReadOnlyList<ChatAttachment> currentTurnAttachments,
    CancellationToken cancellationToken)
  {
    var referencedImages = await ResolveReferencedImagesAsync(
      userMessage, classification, memory, threadAttachments, currentTurnAttachments, cancellationToken);

    var scenario = classification.Scenario ?? memory.Scenario;

    // If we have images to analyze, return with image references for the composer
    if (referencedImages.Count > 0)
    {
      var fallback = scenario is not null
        ? fallbackTextService.Pick("image_analysis_acknowledged", ("scenario", scenario.Replace('-', ' ')))
        : fallbackTextService.Pick("image_analysis_need_scenario");

      return new AssistantTurn(fallback, null, referencedImages);
    }

    return new AssistantTurn(
      fallbackTextService.Pick("image_analysis_missing"),
      null);
  }

  private async Task<IReadOnlyList<ChatAttachment>> SaveIncomingAttachmentsAsync(
    long threadId,
    long messageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken)
  {
    if (attachments.Count == 0)
    {
      return [];
    }

    var saved = new List<ChatAttachment>(attachments.Count);
    foreach (var attachment in attachments)
    {
      var extension = Path.GetExtension(attachment.OriginalFileName);
      if (string.IsNullOrWhiteSpace(extension))
      {
        extension = attachment.MimeType switch
        {
          "image/png" => ".png",
          "image/jpeg" => ".jpg",
          "image/webp" => ".webp",
          _ => ".bin"
        };
      }

      var fileName = $"{Guid.NewGuid():N}{extension}";
      var absoluteDirectory = uploadStoragePathResolver.GetChatThreadDirectory(threadId);
      Directory.CreateDirectory(absoluteDirectory);
      await File.WriteAllBytesAsync(Path.Combine(absoluteDirectory, fileName), attachment.Bytes, cancellationToken);

      var entity = new ChatAttachment
      {
        ThreadId = threadId,
        MessageId = messageId,
        Kind = attachment.Kind,
        FileUrl = $"/upload/chat/{threadId}/{fileName}",
        MimeType = attachment.MimeType,
        OriginalFileName = attachment.OriginalFileName,
        FileSizeBytes = attachment.Bytes.LongLength
      };
      dbContext.ChatAttachments.Add(entity);
      saved.Add(entity);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return saved;
  }

  private async Task<ChatAttachment> SaveGeneratedTryOnAttachmentAsync(
    long threadId,
    AiTryOnResultDto result,
    CancellationToken cancellationToken)
  {
    var (mimeType, bytes) = DecodeDataUrl(result.ResultImageUrl);
    var extension = mimeType switch
    {
      "image/jpeg" => ".jpg",
      "image/webp" => ".webp",
      _ => ".png"
    };
    var fileName = $"tryon-{Guid.NewGuid():N}{extension}";
    var absoluteDirectory = uploadStoragePathResolver.GetChatThreadDirectory(threadId);
    Directory.CreateDirectory(absoluteDirectory);
    await File.WriteAllBytesAsync(Path.Combine(absoluteDirectory, fileName), bytes, cancellationToken);

    var attachment = new ChatAttachment
    {
      ThreadId = threadId,
      Kind = "tryon_result",
      FileUrl = $"/upload/chat/{threadId}/{fileName}",
      MimeType = mimeType,
      OriginalFileName = fileName,
      FileSizeBytes = bytes.LongLength
    };
    dbContext.ChatAttachments.Add(attachment);
    await dbContext.SaveChangesAsync(cancellationToken);
    return attachment;
  }

  private async Task<byte[]> ReadStoredAttachmentBytesAsync(string fileUrl, CancellationToken cancellationToken)
  {
    if (!uploadStoragePathResolver.TryGetAbsolutePathForRequestPath(fileUrl, out var absolutePath))
    {
      throw new InvalidOperationException("Đường dẫn ảnh đã lưu trong chat không hợp lệ.");
    }

    if (!File.Exists(absolutePath))
    {
      throw new InvalidOperationException("Không tìm thấy ảnh người mặc đã lưu trên máy chủ. Vui lòng gửi lại ảnh.");
    }

    return await File.ReadAllBytesAsync(absolutePath, cancellationToken);
  }

  private static (string MimeType, byte[] Bytes) DecodeDataUrl(string dataUrl)
  {
    var separator = dataUrl.IndexOf(',', StringComparison.Ordinal);
    var header = dataUrl[..separator];
    var mimeType = header.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase);
    return (mimeType, Convert.FromBase64String(dataUrl[(separator + 1)..]));
  }

  private static string? HashGuestKey(string? guestKey)
  {
    if (string.IsNullOrWhiteSpace(guestKey))
    {
      return null;
    }

    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(guestKey.Trim()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  private static ChatThreadSummaryDto MapThreadSummary(ChatThread thread)
  {
    var title = BuildThreadTitle(thread.Messages);
    var preview = thread.Messages.OrderByDescending(message => message.CreatedAt).FirstOrDefault()?.Content;
    return new ChatThreadSummaryDto(thread.Id, title, preview, thread.Status, thread.UpdatedAt);
  }

  private static ChatThreadDetailDto MapThreadDetail(ChatThread thread)
  {
    var groupedAttachments = thread.Attachments
      .Where(attachment => attachment.MessageId.HasValue)
      .GroupBy(attachment => attachment.MessageId!.Value)
      .ToDictionary(group => group.Key, group => group.ToList() as IReadOnlyList<ChatAttachment>);

    var messages = thread.Messages
      .OrderBy(message => message.CreatedAt)
      .Select(message => MapMessage(message, groupedAttachments.TryGetValue(message.Id, out var attachments) ? attachments : []))
      .ToList();

    return new ChatThreadDetailDto(
      thread.Id,
      BuildThreadTitle(thread.Messages),
      thread.Status,
      thread.Source,
      thread.CreatedAt,
      thread.UpdatedAt,
      messages);
  }

  private static ChatMessageDto MapMessage(ChatMessage message, IReadOnlyList<ChatAttachment> attachments)
  {
    ChatStructuredPayloadDto? payload = null;
    if (!string.IsNullOrWhiteSpace(message.StructuredPayloadJsonb))
    {
      try
      {
        payload = JsonSerializer.Deserialize<ChatStructuredPayloadDto>(message.StructuredPayloadJsonb, JsonOptions);
      }
      catch (JsonException)
      {
        payload = null;
      }
    }

    return new ChatMessageDto(
      message.Id,
      message.Role,
      message.Content,
      message.Intent,
      message.CreatedAt,
      attachments.Select(MapAttachment).ToList(),
      payload);
  }

  private static ChatAttachmentDto MapAttachment(ChatAttachment attachment) =>
    new(
      attachment.Id,
      attachment.Kind,
      attachment.FileUrl,
      attachment.MimeType,
      attachment.OriginalFileName,
      attachment.CreatedAt);

  private static string BuildThreadTitle(IEnumerable<ChatMessage> messages)
  {
    var firstUser = messages.FirstOrDefault(message => message.Role == UserRole)?.Content;
    if (string.IsNullOrWhiteSpace(firstUser))
    {
      return "Stylist mới";
    }

    return firstUser.Length <= 48 ? firstUser : string.Concat(firstUser.AsSpan(0, 48), "...");
  }

  private string BuildRecommendationCopy(
    IReadOnlyList<ChatRecommendationItemDto> garmentProducts,
    IReadOnlyList<ChatRecommendationItemDto> accessoryProducts,
    string? scenario,
    bool noveltyFirst)
  {
    var intro = string.IsNullOrWhiteSpace(scenario)
      ? fallbackTextService.Pick("recommendation_intro_plain")
      : fallbackTextService.Pick("recommendation_intro_scenario", ("scenario", scenario.Replace('-', ' ')));
    var sections = new List<string> { intro };

    for (var index = 0; index < garmentProducts.Count; index++)
    {
      var garment = garmentProducts[index];
      var accessory = accessoryProducts.Count == 0
        ? null
        : accessoryProducts[Math.Min(index, accessoryProducts.Count - 1)];
      var lookTitle = $"Look {index + 1} — {BuildLookLabel(index)}";
      var difference = index == 0
        ? (noveltyFirst
          ? "ưu tiên mẫu chưa trùng với các gợi ý trước"
          : "ưu tiên độ hợp bối cảnh và dễ mặc")
        : BuildDifferenceReason(garment, accessory);
      var stylingTip = accessory is null
        ? fallbackTextService.Pick("styling_tip_plain")
        : fallbackTextService.Pick("styling_tip_pair", ("accessoryName", accessory.Name.ToLowerInvariant()));

      sections.Add(string.Join("\n", new[]
      {
        lookTitle,
        $"- Áo dài chủ đạo: {garment.Name}",
        $"- Điểm hợp: {garment.Rationale}",
        accessory is null
          ? "- Phụ kiện đi kèm: hiện mình chưa thấy món đủ hợp để ghép ngay trong lượt này."
          : $"- Phụ kiện đi kèm: {accessory.Name} — {accessory.Rationale}",
        $"- Khác biệt: {difference}",
        $"- {stylingTip}"
      }));
    }

    return string.Join("\n", sections);
  }

  private string BuildLookLabel(int index)
  {
    return index switch
    {
      0 => fallbackTextService.Pick("look_label_0"),
      1 => fallbackTextService.Pick("look_label_1"),
      2 => fallbackTextService.Pick("look_label_2"),
      _ => fallbackTextService.Pick("look_label_other")
    };
  }

  private string BuildDifferenceReason(
    ChatRecommendationItemDto garment,
    ChatRecommendationItemDto? accessory)
  {
    if (accessory is null)
    {
      return fallbackTextService.Pick("difference_reason_plain");
    }

    return fallbackTextService.Pick(
      "difference_reason_pair",
      ("garmentName", garment.Name.ToLowerInvariant()),
      ("accessoryName", accessory.Name.ToLowerInvariant()));
  }

  private static bool ShouldPreferUnseenAlternatives(string userMessage, IntentClassificationDto classification, ThreadMemoryStateDto memory)
  {
    if (memory.ShownProductIds.Count == 0)
    {
      return false;
    }

    if (classification.ReferencedProductIds.Count > 0)
    {
      return false;
    }

    if (classification.WantsDifferentOptions)
    {
      return true;
    }

    if (memory.SelectedGarmentProductId.HasValue)
    {
      return false;
    }

    return classification.Intent is "outfit_recommendation" or "catalog_lookup" or "image_style_analysis";
  }


  private static IReadOnlyList<ChatRecommendationItemDto> SelectProducts(
    IReadOnlyList<ChatRecommendationItemDto> products,
    ThreadMemoryStateDto memory,
    bool preferUnseenAlternatives,
    int limit,
    IReadOnlyList<long>? recentlyUsedProductIds = null,
    ISet<string>? usedOutfitSignatures = null,
    long? anchorProductId = null,
    bool allowSeenFallback = true)
  {
    var rejectedProductIds = memory.RejectedProductIds.ToHashSet();
    var shownProductIds = memory.ShownProductIds.ToHashSet();
    var recentProductIds = recentlyUsedProductIds?.Count > 0 ? recentlyUsedProductIds.ToHashSet() : [];

    var filtered = new List<ChatRecommendationItemDto>();
    var strictFiltered = new List<ChatRecommendationItemDto>();
    var unseen = new List<ChatRecommendationItemDto>();
    var seen = new List<ChatRecommendationItemDto>();

    foreach (var product in products)
    {
      if (rejectedProductIds.Contains(product.ProductId))
      {
        continue;
      }

      filtered.Add(product);

      if (recentProductIds.Contains(product.ProductId) && products.Count > limit)
      {
        continue;
      }

      if (anchorProductId.HasValue && usedOutfitSignatures is not null && usedOutfitSignatures.Contains(BuildOutfitSignature(anchorProductId.Value, [product.ProductId])))
      {
        continue;
      }

      strictFiltered.Add(product);

      if (shownProductIds.Contains(product.ProductId))
      {
        seen.Add(product);
      }
      else
      {
        unseen.Add(product);
      }
    }

    if (strictFiltered.Count == 0)
    {
      strictFiltered = filtered;
      unseen = strictFiltered.Where(product => !shownProductIds.Contains(product.ProductId)).ToList();
      seen = strictFiltered.Where(product => shownProductIds.Contains(product.ProductId)).ToList();
    }

    if (!preferUnseenAlternatives || shownProductIds.Count == 0)
    {
      return unseen.Concat(seen).Take(limit).ToList();
    }

    if (unseen.Count == 0 && allowSeenFallback)
    {
      return strictFiltered.Take(limit).ToList();
    }

    if (!allowSeenFallback)
    {
      return unseen.Take(limit).ToList();
    }

    return unseen.Concat(seen).Take(limit).ToList();
  }

  private static string BuildExhaustedRecommendationCopy(string? scenario)
  {
    var scenarioLabel = string.IsNullOrWhiteSpace(scenario) ? "nhu cầu hiện tại" : scenario.Replace('-', ' ');
    return $"Mình đã đi hết các mẫu áo dài chưa lặp lại cho bối cảnh {scenarioLabel}. Nếu bạn muốn, mình có thể chuyển sang hướng màu khác, khoảng giá khác hoặc phối lại phụ kiện trên các mẫu đã xem.";
  }

  private static string BuildOutfitSignature(long garmentProductId, IReadOnlyList<long> accessoryProductIds)
  {
    var normalizedAccessoryIds = accessoryProductIds.Distinct().OrderBy(id => id);
    return $"{garmentProductId}:{string.Join('-', normalizedAccessoryIds)}";
  }

  private static HashSet<string> BuildOutfitSignatureSet(ThreadMemoryStateDto memory)
  {
    return memory.ShownOutfitSignatures.ToHashSet(StringComparer.Ordinal);
  }

  private static ChatStructuredPayloadDto BuildStructuredPayload(
    string kind,
    string? scenario,
    bool canTryOn,
    bool requiresPersonImage,
    long? selectedGarmentProductId,
    IReadOnlyList<long> selectedAccessoryProductIds,
    IReadOnlyList<string> pendingTryOnRequirements,
    IReadOnlyList<ChatRecommendationItemDto> garmentProducts,
    IReadOnlyList<ChatRecommendationItemDto> accessoryProducts,
    IReadOnlyList<ChatRecommendationItemDto>? legacyProducts = null)
  {
    return new ChatStructuredPayloadDto(
      kind,
      scenario,
      canTryOn,
      requiresPersonImage,
      selectedGarmentProductId,
      selectedAccessoryProductIds,
      pendingTryOnRequirements,
      legacyProducts ?? garmentProducts.Concat(accessoryProducts).ToList(),
      garmentProducts,
      accessoryProducts);
  }

  private static bool ShouldBuildAccessoryCombo(ThreadMemoryStateDto memory, IntentClassificationDto classification)
  {
    var hasGarmentContext = memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0 || memory.GarmentShortlistedProductIds.Count > 0;
    return hasGarmentContext;
  }

  private async Task<IReadOnlyList<ChatRecommendationItemDto>> LoadComboGarmentProductsAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var garmentIds = memory.GarmentShortlistedProductIds.Count > 0
      ? memory.GarmentShortlistedProductIds
      : memory.ShortlistedProductIds;

    if (memory.SelectedGarmentProductId.HasValue)
    {
      garmentIds = [memory.SelectedGarmentProductId.Value, .. garmentIds.Where(id => id != memory.SelectedGarmentProductId.Value)];
    }

    if (garmentIds.Count > 0)
    {
      return await catalogStylingService.CompareAsync(garmentIds.Take(3).ToList(), cancellationToken);
    }

    return await catalogStylingService.RecommendAsync(
      classification.Scenario ?? memory.Scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "ao_dai",
      3,
      null,
      cancellationToken);
  }

  private static string? BuildImageCatalogText(ThreadMemoryStateDto memory)
  {
    return memory.ImageCatalog.Count == 0
      ? null
      : string.Join(", ", memory.ImageCatalog.Select(e => $"{e.Label} ({e.Kind})"));
  }

  private static long? ResolveReferencedPersonImageId(IntentClassificationDto classification, ThreadMemoryStateDto memory)
  {
    if (string.IsNullOrWhiteSpace(classification.ReferencedImageHint))
    {
      return memory.LatestPersonAttachmentId;
    }

    var entry = ResolveImageCatalogEntries(memory, classification.ReferencedImageHint).FirstOrDefault();
    return entry?.Kind == "user_image" ? entry.AttachmentId : null;
  }

  private static IEnumerable<ImageCatalogEntry> ResolveImageCatalogEntries(ThreadMemoryStateDto memory, string? hint)
  {
    var catalog = memory.ImageCatalog;
    if (catalog.Count == 0)
    {
      return [];
    }

    return hint switch
    {
      "first" => [catalog.First()],
      "last" => [catalog.Last()],
      "tryon_result" => catalog.Where(e => e.Kind == "tryon_result").TakeLast(1),
      var value when !string.IsNullOrWhiteSpace(value)
        && value.StartsWith("image_", StringComparison.Ordinal)
        && int.TryParse(value[6..], out var hintIndex)
        && hintIndex > 0
        && hintIndex <= catalog.Count => [catalog[hintIndex - 1]],
      _ => [catalog.Last()]
    };
  }

  private async Task<IReadOnlyList<ImageReferenceDto>> ResolveReferencedImagesAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    IReadOnlyList<ChatAttachment> threadAttachments,
    IReadOnlyList<ChatAttachment>? currentTurnAttachments,
    CancellationToken cancellationToken)
  {
    var result = new List<ImageReferenceDto>();

    if (currentTurnAttachments is not null)
    {
      foreach (var attachment in currentTurnAttachments.Where(a => a.Kind is "user_image" or "tryon_result"))
      {
        var entry = memory.ImageCatalog.FirstOrDefault(e => e.AttachmentId == attachment.Id);
        var bytes = await ReadStoredAttachmentBytesAsync(attachment.FileUrl, cancellationToken);
        result.Add(new ImageReferenceDto(
          attachment.Id,
          entry?.Label ?? $"Ảnh {attachment.Id}",
          attachment.Kind,
          attachment.MimeType,
          bytes));
      }

      if (result.Count > 0)
      {
        return result;
      }
    }

    var matched = ResolveImageCatalogEntries(memory, classification.ReferencedImageHint);

    foreach (var entry in matched)
    {
      var attachment = threadAttachments.FirstOrDefault(a => a.Id == entry.AttachmentId);
      if (attachment is null)
      {
        continue;
      }

      var bytes = await ReadStoredAttachmentBytesAsync(attachment.FileUrl, cancellationToken);
      result.Add(new ImageReferenceDto(attachment.Id, entry.Label, entry.Kind, attachment.MimeType, bytes));
    }

    return result;
  }

  private sealed record AssistantTurn(string Content, ChatStructuredPayloadDto? StructuredPayload, IReadOnlyList<ImageReferenceDto>? ReferencedImages = null);
}
